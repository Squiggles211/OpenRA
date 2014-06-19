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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class GameStatsLogic
	{
		readonly Widget rootMenu;

		[ObjectCreator.UseCtor]
		public GameStatsLogic(Widget widget, Action onExit)
		{
			rootMenu = widget;

			rootMenu.Get<ButtonWidget>("STATS_CLOSE").OnClick = () =>
			{
				//Nullify to reset after showing the previous game's stats
				PlayerStatistics.previousGameStats = null;

				//Close this window and run the onExit action
				Ui.CloseWindow();
				onExit();
			};
		}
	}
}
