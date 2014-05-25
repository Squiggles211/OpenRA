#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA.Buildings
{
	public class PlacementSchemeInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PlacementScheme(init, this); }

		public virtual Order place()
		{
			return new Order();
		}
	}

	public class PlacementScheme
	{
		public PlacementScheme(ActorInitializer init, PlacementSchemeInfo info) { }
	}
}
