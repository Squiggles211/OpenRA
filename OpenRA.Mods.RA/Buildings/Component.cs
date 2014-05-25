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
	public class ComponentInfo : ITraitInfo
	{
		[Desc("What buildings are you allowed to place this component on.")]
		public readonly string[] Upgradables = {};

		public object Create(ActorInitializer init) { return new Component(init, this); }
	}

	public class Component : INotifyCapture, INotifySold, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public readonly ComponentInfo Info;
		readonly Actor self;

		public Component(ActorInitializer init, ComponentInfo info)
		{
			this.self = init.self;
			this.Info = info;
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
		}

		public void AddedToWorld(Actor self)
		{
			//self.World.ActorMap.AddInfluence(self, this);
			//self.World.ActorMap.AddPosition(self, this);
			//self.World.ScreenMap.Add(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			//self.World.ActorMap.RemoveInfluence(self, this);
			//self.World.ActorMap.RemovePosition(self, this);
			//self.World.ScreenMap.Remove(self);
		}

		public void Selling(Actor self) { }
		public void Sold(Actor self) { }
	}
}
