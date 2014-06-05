#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA.Move
{
	[Desc("Unit is able to move.")]
	public class SubterraneanMobileInfo : MobileInfo
	{
		[Desc("Speed when moving underground.")]
		public readonly int SubterraneanSpeed = 1;

		[Desc("Number of ticks it takes to burrow/unburrow")]
		public readonly int BurrowTime = 14; //approximately .22 seconds as per rules.ini

		public override object Create(ActorInitializer init) { return new SubterraneanMobile(init, this); }
	}

	public class SubterraneanMobile : Mobile
	{
		[Sync]
		public bool IsSubterranean = false;

		public SubterraneanMobile(ActorInitializer init, MobileInfo info) : base(init, info)
		{

		}

		public override IEnumerable<IOrderTargeter> Orders { get { yield return new SubterraneanMoveOrderTargeter(self, Info); } }

		// Note: Returns a valid order even if the unit can't move to the target
		public override Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is SubterraneanMoveOrderTargeter)
			{
				if (Info.OnRails)
					return null;

				return new Order("Move", self, queued) { TargetLocation = target.CenterPosition.ToCPos() };
			}
			return null;
		}

		/*public CPos NearestMoveableCell(CPos target, int minRange, int maxRange)
		{
			if (CanEnterCell(target))
				return target;

			var searched = new List<CPos>();
			// Limit search to a radius of 10 tiles
			for (int r = minRange; r < maxRange; r++)
				foreach (var tile in self.World.FindTilesInCircle(target, r).Except(searched))
				{
					if (CanEnterCell(tile))
						return tile;

					searched.Add(tile);
				}

			// Couldn't find a cell
			return target;
		}*/

		/*public CPos NearestCell(CPos target, Func<CPos, bool> check, int minRange, int maxRange)
		{
			if (check(target))
				return target;

			var searched = new List<CPos>();
			for (int r = minRange; r < maxRange; r++)
				foreach (var tile in self.World.FindTilesInCircle(target, r).Except(searched))
				{
					if (check(tile))
						return tile;

					searched.Add(tile);
				}

			// Couldn't find a cell
			return target;
		}*/

		protected override void PerformMove(Actor self, CPos targetLocation, bool queued)
		{
			if (!IsSubterranean)
			{
				//not currently underground, determine if we should go underground, does not matter if this is queued or not, queue the dig and move underground

				//self.TraitOrDefault<RenderSprites>().Add("dig", new Graphics.AnimationWithOffset(new Graphics.Animation(world, "dig"), 
				base.PerformMove(self, targetLocation, queued);
			}
			else
			{
				//currently underground, any move we do now stays underground, including queued movements

				self.TraitOrDefault<RenderUnit>().PlayCustomAnim(self, "dig");
				//determine if we should traverse to destination by burrowing

				//CostA = cost to find burrowable terrain to enter (0 if current cell is burrowable)
				//CostB = cost to traverse underground (should be relatively low cost for underground)
				//CostC = cost to find burrowable terrain to exit (0 if target is burrowable)

				//CostD = cost to use normal methods of pathing to target destination

				//if (CostD > CostA + CostB + CostC)
				//IsSubterranean = true;
				//CreateInner move activity underground..
				//else
				//base.PerformMove(self, targetLocation, queued);

				base.PerformMove(self, targetLocation, queued);
			}
		}

		class SubterraneanMoveOrderTargeter : IOrderTargeter
		{
			readonly MobileInfo unitType;
			readonly bool rejectMove;

			public SubterraneanMoveOrderTargeter(Actor self, MobileInfo unitType)
			{
				this.unitType = unitType;
				this.rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || !target.IsValidFor(self))
					return false;

				var location = target.CenterPosition.ToCPos();
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				cursor = "move";

				if (self.Owner.Shroud.IsExplored(location))
					cursor = self.World.GetTerrainInfo(location).CustomCursor ?? cursor;

				if (!self.World.Map.IsInMap(location) || (self.Owner.Shroud.IsExplored(location) &&
						unitType.MovementCostForCell(self.World, location) == int.MaxValue))
					cursor = "move-blocked";

				return true;
			}
		}

		/*public Activity ScriptedMove(CPos cell) { return new Move(cell); }
		public Activity MoveTo(CPos cell, int nearEnough) { return new Move(cell, nearEnough); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return new Move(cell, ignoredActor); }
		public Activity MoveWithinRange(Target target, WRange range) { return new Move(target, range); }
		public Activity MoveFollow(Actor self, Target target, WRange range) { return new Follow(self, target, range); }
		public Activity MoveTo(Func<List<CPos>> pathFunc) { return new Move(pathFunc); }*/

		public override Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return base.VisualMove(self, fromPos, toPos);
		}
    }
}
