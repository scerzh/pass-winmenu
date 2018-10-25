﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using McSherry.SemanticVersioning;

namespace PassWinmenu.UpdateChecking
{
	internal class UpdateChecker : IDisposable
	{

		public IUpdateSource UpdateSource { get; }
		public SemanticVersion CurrentVersion { get; }
		public ProgramVersion? LatestVersion { get; private set; }
		public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

		public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

		private Timer timer;

		public UpdateChecker(IUpdateSource updateSource, SemanticVersion currentVersion)
		{
			UpdateSource = updateSource;
			CurrentVersion = currentVersion;
		}

		/// <summary>
		/// Starts checking for updates.
		/// </summary>
		public void Start()
		{
			timer = new Timer(CheckInterval.TotalMilliseconds)
			{
				AutoReset = true,
			};

			timer.Elapsed += (sender, args) =>
			{
				CheckForUpdates();
			};
			timer.Start();
		}

		/// <summary>
		/// Checks for any available updates, raising <see cref="UpdateAvailable"/> if an update is found.
		/// </summary>
		public void CheckForUpdates()
		{
			var update = GetUpdate();
			if (update == null) return;

			// Stop automatic update checking if we've found an update.
			timer.Stop();
			timer.Dispose();
			LatestVersion = update;
			NotifyUpdate(update.Value);
		}


		/// <summary>
		/// Checks for an available update.
		/// </summary>
		/// <returns>
		/// A <see cref="ProgramVersion"/> representing the available update,
		/// or <value>nul</value> if no update is available.
		/// </returns>
		private ProgramVersion? GetUpdate()
		{
			if (UpdateSource.RequiresConnectivity && !GetConnectivity())
			{
				return null;
			}

			var latestVersion = UpdateSource.GetLatestVersion();

			if (latestVersion.VersionNumber > CurrentVersion)
			{
				return latestVersion;
			}
			return null;
		}

		/// <summary>
		/// Makes a lightweight HTTP request to Google to check whether an internet connection is available.
		/// </summary>
		private bool GetConnectivity()
		{
			try
			{
				using (var client = new WebClient())
				using (client.OpenRead("http://clients3.google.com/generate_204"))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Dispatches the <see cref="UpdateAvailable"/> event to notify listeners that an update is available.
		/// </summary>
		/// <param name="version">A <see cref="ProgramVersion"/> representing the new version.</param>
		private void NotifyUpdate(ProgramVersion version)
		{
			UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(version));
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			timer?.Dispose();
		}
	}

	internal class UpdateAvailableEventArgs : EventArgs
	{
		public ProgramVersion Version { get; set; }

		public UpdateAvailableEventArgs(ProgramVersion version)
		{
			Version = version;
		}
	}
}