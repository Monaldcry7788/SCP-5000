﻿using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs;
using MEC;
using System;
using UnityEngine;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace SCP5000.Component
{
    internal class PlayerComponent : MonoBehaviour
    {
        internal Player Player { get; private set; }

        private void Awake()
        {
            SubscribeEvents();
            Player = Player.Get(gameObject);
            API.SCP5000API.Players.Add(Player);
        }

        private void Start()
        {
            if (Player.Role != SCP5000.Singleton.Config.Role)
                Player.Role = SCP5000.Singleton.Config.Role;
            Player.Broadcast(SCP5000.Singleton.Config.SpawnBroadcast.Duration, SCP5000.Singleton.Config.SpawnBroadcast.Content.Replace("{player}", Player.Nickname), Broadcast.BroadcastFlags.Normal, true);
            Timing.CallDelayed(0.5f, () => Player.ResetInventory(SCP5000.Singleton.Config.Inventory));
            Player.Ammo.Add(ItemType.Ammo762x39, 40);
            if (SCP5000.Singleton.Config.EnableEffect)
            {
                Player.EnableEffect(EffectType.Disabled);
                Player.EnableEffect(EffectType.Deafened);
            }
            Player.IsBypassModeEnabled = true;
            Player.Health = Player.MaxHealth = SCP5000.Singleton.Config.HP;
            Cassie.Message(SCP5000.Singleton.Config.SpawnCassie, false, true);
            SetBadge();
        }

        private void FixedUpdate()
        {
            if (Player is null || Player.Role != SCP5000.Singleton.Config.Role)
                Destroy();
        }

        private void PartiallyDestroy()
        {
            UnsubscribeEvents();
            API.SCP5000API.Players.Remove(Player);
            Player.IsBypassModeEnabled = false;
            Player.RankName = default;
            Player.RankColor = default;
        }

        private void OnDestroy() => PartiallyDestroy();

        public void Destroy()
        {
            try
            {
                Destroy(this);
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't destroy PlayerComponent: {e}");
            }
        }

        private void SubscribeEvents()
        {
            PlayerEvents.Died += OnDied;
            PlayerEvents.TriggeringTesla += OnTriggeringTesla;
            PlayerEvents.EnteringFemurBreaker += OnEnteringFemurBreaker;
            PlayerEvents.Dying += OnDying;
            Exiled.Events.Handlers.Scp096.AddingTarget += OnAddingTarget;
        }

        private void UnsubscribeEvents()
        {
            PlayerEvents.Died -= OnDied;
            PlayerEvents.TriggeringTesla -= OnTriggeringTesla;
            PlayerEvents.EnteringFemurBreaker -= OnEnteringFemurBreaker;
            PlayerEvents.Dying -= OnDying;
            Exiled.Events.Handlers.Scp096.AddingTarget -= OnAddingTarget;
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (ev.Target != Player) return;
            API.SCP5000API.Players.Remove(Player);
            Cassie.Message(SCP5000.Singleton.Config.RecontainCassie, false, true);
            Destroy();
        }

        private void SetBadge()
        {
            Player.BadgeHidden = false;
            Player.RankName = SCP5000.Singleton.Config.Badge;
            Player.RankColor = SCP5000.Singleton.Config.Color;
        }

        private void OnTriggeringTesla(TriggeringTeslaEventArgs ev)
        {
            if (ev.Player != Player || SCP5000.Singleton.Config.TeslaTriggerable) return;

            ev.IsTriggerable = false;
            Player.Broadcast(SCP5000.Singleton.Config.TeslaBroadcast.Duration, SCP5000.Singleton.Config.TeslaBroadcast.Content.Replace("{player}", ev.Player.Nickname), Broadcast.BroadcastFlags.Normal, true);
        }

        private void OnEnteringFemurBreaker(EnteringFemurBreakerEventArgs ev)
        {
            if (ev.Player != Player || SCP5000.Singleton.Config.FemurBreakerTriggerable) return;

            ev.IsAllowed = false;
            Player.Broadcast(SCP5000.Singleton.Config.FemurBreakerBroadcast.Duration, SCP5000.Singleton.Config.FemurBreakerBroadcast.Content.Replace("{player}", ev.Player.Nickname), Broadcast.BroadcastFlags.Normal, true);
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (ev.Target != Player || !SCP5000.Singleton.Config.ExplosionEnable) return;

            Cassie.Message(SCP5000.Singleton.Config.ExplosionCassie);
            new ExplosiveGrenade(ItemType.GrenadeHE, Player) { FuseTime = SCP5000.Singleton.Config.FuseTime }.SpawnActive(Player.Position, Player);
        }

        private void OnAddingTarget(AddingTargetEventArgs ev)
        {
            if (ev.Target != Player || SCP5000.Singleton.Config.AddingTarget) return;

            ev.IsAllowed = false;
            Player.Broadcast(SCP5000.Singleton.Config.AddingTargetBroadcast.Duration, SCP5000.Singleton.Config.AddingTargetBroadcast.Content, Broadcast.BroadcastFlags.Normal, true);
        }
    }
}