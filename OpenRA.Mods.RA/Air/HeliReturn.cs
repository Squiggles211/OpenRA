#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class HeliReturn : Activity
	{
		static Actor ChooseHelipad(Actor self)
		{
			var rearmBuildings = self.Info.Traits.Get<HelicopterInfo>().RearmBuildings;
			return self.World.Actors.Where(a => a.Owner == self.Owner).FirstOrDefault(
				a => rearmBuildings.Contains(a.Info.Name) && !Reservable.IsReserved(a));
		}

		public static Actor NearestHelipad(Actor self)
		{
			var rearmBuildings = self.Info.Traits.Get<HelicopterInfo>().RearmBuildings;

			return self.World.ActorsWithTrait<Reservable>()
					.Where(a => a.Actor.Owner == self.Owner && rearmBuildings.Contains(a.Actor.Info.Name))
					.Select(a => a.Actor)
					.ClosestTo(self);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var dest = ChooseHelipad(self);
			var initialFacing = self.Info.Traits.Get<AircraftInfo>().InitialFacing;

			if (dest == null)
			{
				var nearestHpad = NearestHelipad(self);

				//Should not be null unless the helipad that we would have attempted to return to was destroyed in the same tick as this activity
				if (nearestHpad == null)
					return NextActivity;
				else
					return Util.SequenceActivities(new HeliFly(self, Target.FromActor(nearestHpad)));
			}

			var res = dest.TraitOrDefault<Reservable>();
			var heli = self.Trait<Helicopter>();

			if (res != null)
			{
				heli.UnReserve();
				heli.Reservation = res.Reserve(dest, self, heli);
			}
			var exit = dest.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
			var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

			return Util.SequenceActivities(
				new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)),
				new Turn(initialFacing),
				new HeliLand(false),
				new ResupplyAircraft(),
				new TakeOff());
		}
	}
}
