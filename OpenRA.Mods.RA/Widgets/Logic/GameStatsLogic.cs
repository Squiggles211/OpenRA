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
		readonly LineGraphWidget graph;

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

			graph = rootMenu.Get<LineGraphWidget>("STATS_GRAPH");

			graph.GetSeries = () =>
				PlayerStatistics.previousGameStats.Select(p => new LineGraphSeries(p.playerName, p.playerColor, p.EarnedSamples.Select(s => (float)s)));

			graph.GetXAxisSize = () =>
				PlayerStatistics.previousGameStats.Select(p => p.EarnedSamples.Count).Max();

			rootMenu.Get<ButtonWidget>("STRUCTURES_TAB").OnClick = () =>
			{
				//change series to get buildings info
				graph.GetSeries = () =>
					PlayerStatistics.previousGameStats.Select(p => new LineGraphSeries(p.playerName, p.playerColor, p.BuiltBuildings.Select(b => (float)b)));

				graph.GetXAxisSize = () =>
					PlayerStatistics.previousGameStats.Select(p => p.BuiltBuildings.Count).Max();

				graph.GetYAxisSize = () =>
					(PlayerStatistics.previousGameStats.Select(p => p.BuiltBuildings.Count).Max() * 4 / 3) + 1;

				graph.GetYAxisValueFormat = () => "{0}";
			};

			rootMenu.Get<ButtonWidget>("RESOURCES_TAB").OnClick = () =>
			{
				graph.GetSeries = () =>
					PlayerStatistics.previousGameStats.Select(p => new LineGraphSeries(p.playerName, p.playerColor, p.EarnedSamples.Select(s => (float)s)));

				graph.GetXAxisSize = () =>
					PlayerStatistics.previousGameStats.Select(p => p.EarnedSamples.Count).Max();
			};
		}
	}
}
