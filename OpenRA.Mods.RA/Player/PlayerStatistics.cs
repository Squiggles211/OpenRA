#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class PlayerStatisticsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PlayerStatistics(init.self); }
	}

	public class PlayerStatistics : ITick, IResolveOrder
	{
		public static PlayerStatistics[] previousGameStats { get; set; }
		World world;
		Player player;

		public readonly string playerName;
		public readonly Color playerColor;

		public double MapControl;
		public int OrderCount;

		public int EarnedThisMinute 
		{
			get
			{
				return player.PlayerActor.Trait<PlayerResources>().Earned;
			}
		}

		public List<int> EarnedSamples = new List<int>();
		public List<int> BuiltBuildings = new List<int>();
		public List<int> BuiltUnits = new List<int>();

		public int KillsCost;
		public int DeathsCost;

		public int UnitsKilled;
		public int UnitsDead;

		public int BuildingsKilled;
		public int BuildingsDead;

		public int currentBuildings;
		public int currentUnits;

		public PlayerStatistics(Actor self)
		{
			world = self.World;
			player = self.Owner;

			playerName = player.PlayerName;
			playerColor = player.Color.RGB;

			currentBuildings = world.Actors.Where(a => a.IsInWorld && a.Owner == self.Owner && a.HasTrait<Building>() && !a.IsDead()).Count();
			BuiltBuildings.Add(currentBuildings);

			currentUnits = 0;
		}

		void UpdateMapControl()
		{
			var total = (double)world.Map.Bounds.Width * world.Map.Bounds.Height;
			MapControl = world.Actors
				.Where(a => !a.IsDead() && a.IsInWorld && a.Owner == player && a.HasTrait<RevealsShroud>())
				.SelectMany(a => world.FindTilesInCircle(a.Location, a.Trait<RevealsShroud>().Range.Clamp(WRange.Zero, WRange.FromCells(50)).Range / 1024))
				.Distinct()
				.Count() / total;
		}

		void UpdateEarnedThisMinute()
		{
			EarnedSamples.Add(EarnedThisMinute);
		}

		public void Tick(Actor self)
		{
			//Update every 10 seconds
			if (self.World.WorldTick % 250 == 1)
				UpdateEarnedThisMinute();
			if (self.World.WorldTick % 250 == 0)
				UpdateMapControl();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Chat":
				case "TeamChat":
				case "HandshakeResponse":
				case "PauseGame":
				case "StartGame":
				case "Disconnected":
				case "ServerError":
				case "AuthenticationError":
				case "SyncLobbyInfo":
				case "SyncClientInfo":
				case "SyncLobbySlots":
				case "SyncLobbyGlobalSettings":
				case "SyncClientPing":
				case "Ping":
				case "Pong":
					return;
			}
			if (order.OrderString.StartsWith("Dev"))
				return;
			OrderCount++;
		}
	}

	public class UpdatesPlayerStatisticsInfo : TraitInfo<UpdatesPlayerStatistics> { }

	public class UpdatesPlayerStatistics : INotifyKilled, INotifyBuildingPlaced
	{
		public void Killed(Actor self, AttackInfo e)
		{
			var attackerStats = e.Attacker.Owner.PlayerActor.Trait<PlayerStatistics>();
			var defenderStats = self.Owner.PlayerActor.Trait<PlayerStatistics>();

			if (self.HasTrait<Building>())
			{
				attackerStats.BuildingsKilled++;
				defenderStats.BuildingsDead++;

				defenderStats.BuiltBuildings.Add(--defenderStats.currentBuildings);
			}
			else if (self.HasTrait<IPositionable>())
			{
				attackerStats.UnitsKilled++;
				defenderStats.UnitsDead++;

				defenderStats.BuiltUnits.Add(--defenderStats.currentUnits);
			}

			if (self.HasTrait<Valued>())
			{
				var cost = self.Info.Traits.Get<ValuedInfo>().Cost;
				attackerStats.KillsCost += cost;
				defenderStats.DeathsCost += cost;
			}
		}

		public void BuildingPlaced(Actor building)
		{
			var playerStats = building.Owner.PlayerActor.Trait<PlayerStatistics>();

			playerStats.BuiltBuildings.Add(++playerStats.currentBuildings);
		}
	}
}
