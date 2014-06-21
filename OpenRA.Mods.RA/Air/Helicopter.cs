#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class HelicopterInfo : AircraftInfo, IMoveInfo
	{
		public readonly WRange IdealSeparation = new WRange(1706);
		public readonly bool LandWhenIdle = true;
		public readonly bool TurnToLand = false;
		public readonly WRange LandAltitude = WRange.Zero;
		public readonly WRange AltitudeVelocity = new WRange(100);

		public override object Create(ActorInitializer init) { return new Helicopter(init, this); }
	}

	class Helicopter : Aircraft, ITick, IResolveOrder, IMove
	{
		public HelicopterInfo Info;
		Actor self;
		bool firstTick = true;
		public bool IsMoving { get { return self.CenterPosition.Z > 0; } set { } }

		public Helicopter(ActorInitializer init, HelicopterInfo info)
			: base(init, info)
		{
			self = init.self;
			Info = info;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			//Always attempt to remove reservation as precaution
			if (Reservation != null)
			{
				Reservation.Dispose();
				Reservation = null;
			}

			bool queued = order.Queued && !self.IsIdle;

			if (order.OrderString == "Move")
			{
				if(!queued)
					self.CancelActivity();

				self.QueueActivity(new CallFunc(() =>
				{
					var cell = self.World.Map.Clamp(order.TargetLocation);
					var t = Target.FromCell(cell);

					//queue the target line setting
					self.QueueActivity(new CallFunc(() => self.SetTargetLine(t, Color.Green)));
					self.QueueActivity(new HeliFly(self, t));

					//queue landing tasks in order to make the decision to land only if there is no next activity..
					self.QueueActivity(new CallFunc(() =>
					{
						//Since current activity is still HeliFly at this point, check the activity after the next one
						if (Info.LandWhenIdle && self.GetCurrentActivity().NextActivity != null && self.GetCurrentActivity().NextActivity.NextActivity == null)
						{
							if (Info.TurnToLand)
								self.QueueActivity(new Turn(Info.InitialFacing));

							self.QueueActivity(new HeliLand(true));
						}
					}));
				}));
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor))
				{
					self.CancelActivity();
					self.QueueActivity(new HeliReturn());
				}
				else
				{
					var res = order.TargetActor.TraitOrDefault<Reservable>();
					if (res != null)
						Reservation = res.Reserve(order.TargetActor, self, this);

					var exit = order.TargetActor.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
					var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

					self.SetTargetLine(Target.FromActor(order.TargetActor), Color.Green);

					self.CancelActivity();
					self.QueueActivity(new HeliFly(self, Target.FromPos(order.TargetActor.CenterPosition + offset)));
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(false));
					self.QueueActivity(new ResupplyAircraft());
					self.QueueActivity(new TakeOff());
				}
			}

			if (order.OrderString == "ReturnToBase")
			{
				//Only return if there is an available helipad, prevents interrupting current activity for any helicopter if not available.
				if (HeliReturn.NearestHelipad(self) != null)
				{
					self.CancelActivity();
					self.QueueActivity(new HeliReturn());
				}
			}

			if (order.OrderString == "Stop")
			{
				self.CancelActivity();

				if (Info.LandWhenIdle)
				{
					if (Info.TurnToLand)
						self.QueueActivity(new Turn(Info.InitialFacing));

					self.QueueActivity(new HeliLand(true));
				}
			}
		}

		public void Tick(Actor self)
		{
			if (firstTick)
			{
				firstTick = false;
				if (!self.HasTrait<FallsToEarth>()) // TODO: Aircraft husks don't properly unreserve.
					ReserveSpawnBuilding();

				var host = GetActorBelow();
				if (host == null)
					return;

				self.QueueActivity(new TakeOff());
			}

			// Repulsion only applies when we're flying!
			var altitude = CenterPosition.Z;
			if (altitude != Info.CruiseAltitude.Range)
				return;

			var otherHelis = self.World.FindActorsInCircle(self.CenterPosition, Info.IdealSeparation)
				.Where(a => a.HasTrait<Helicopter>());

			var f = otherHelis
				.Select(h => GetRepulseForce(self, h))
				.Aggregate(WVec.Zero, (a, b) => a + b);

			var repulsionFacing = Util.GetFacing(f, -1);
			if (repulsionFacing != -1)
				SetPosition(self, CenterPosition + FlyStep(repulsionFacing));
		}

		public WVec GetRepulseForce(Actor self, Actor other)
		{
			if (self == other || other.CenterPosition.Z < self.CenterPosition.Z)
				return WVec.Zero;

			var d = self.CenterPosition - other.CenterPosition;
			var distSq = d.HorizontalLengthSquared;
			if (distSq > Info.IdealSeparation.Range * Info.IdealSeparation.Range)
				return WVec.Zero;

			if (distSq < 1)
			{
				var yaw = self.World.SharedRandom.Next(0, 1023);
				var rot = new WRot(WAngle.Zero, WAngle.Zero, new WAngle(yaw));
				return new WVec(1024, 0, 0).Rotate(rot);
			}

			return (d * 1024 * 8) / (int)distSq;
		}

		public Activity MoveTo(CPos cell, int nearEnough)
		{
			if (Info.LandWhenIdle)
				return (Info.TurnToLand ? Util.SequenceActivities(new HeliFly(self, Target.FromCell(cell)), new Turn(Info.InitialFacing), new HeliLand(true)) :
					Util.SequenceActivities(new HeliFly(self, Target.FromCell(cell)), new HeliLand(true)));

			return new HeliFly(self, Target.FromCell(cell));
		}

		public Activity MoveTo(CPos cell, Actor ignoredActor)
		{
			return MoveTo(cell, 0);
		}

		public Activity MoveWithinRange(Target target, WRange range)
		{
			if (Info.LandWhenIdle)
				return (Info.TurnToLand ? Util.SequenceActivities(new HeliFly(self, target, WRange.Zero, range), new Turn(Info.InitialFacing), new HeliLand(true)) : 
					Util.SequenceActivities(new HeliFly(self, target, WRange.Zero, range), new HeliLand(true)));

			return new HeliFly(self, target, WRange.Zero, range);
		}

		public Activity MoveWithinRange(Target target, WRange minRange, WRange maxRange)
		{
			if (Info.LandWhenIdle)
				return (Info.TurnToLand ? Util.SequenceActivities(new HeliFly(self, target, minRange, maxRange), new Turn(Info.InitialFacing), new HeliLand(true)) :
					Util.SequenceActivities(new HeliFly(self, target, minRange, maxRange), new HeliLand(true)));

			return new HeliFly(self, target, minRange, maxRange);
		}

		public Activity MoveFollow(Actor self, Target target, WRange minRange, WRange maxRange) { return new Follow(self, target, minRange, maxRange); }
		public CPos NearestMoveableCell(CPos cell) { return cell; }

		public Activity MoveIntoWorld(Actor self, CPos cell)
		{
			return new HeliFly(self, Target.FromCell(cell));
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			// TODO: Ignore repulsion when moving
			return Util.SequenceActivities(new CallFunc(() => SetVisualPosition(self, fromPos)), new HeliFly(self, Target.FromPos(toPos)));
		}

		public override IEnumerable<Activity> GetResupplyActivities(Actor a)
		{
			foreach (var b in base.GetResupplyActivities(a))
				yield return b;
		}
	}
}
