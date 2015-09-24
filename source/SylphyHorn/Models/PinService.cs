using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WindowsDesktop;
using VDMHelperCLR.Common;

namespace SylphyHorn.Models
{
	public class PinService : IDisposable
	{
		private readonly object sync = new object();
		private readonly HashSet<IntPtr> pinnedWindows = new HashSet<IntPtr>();
		private readonly IVdmHelper helper;

		public PinService(IVdmHelper helper)
		{
			this.helper = helper;
			VirtualDesktop.CurrentChanged += this.VirtualDesktopOnCurrentChanged;
		}

		public bool Register(IntPtr hWnd)
		{
			lock (this.sync)
			{
				return this.pinnedWindows.Add(hWnd);
			}
		}

		public bool Unregister(IntPtr hWnd)
		{
			lock (this.sync)
			{
				return this.pinnedWindows.Remove(hWnd);
			}
		}

		private void VirtualDesktopOnCurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
		{
			IntPtr[] targets;
			lock (this.sync)
			{
				targets = this.pinnedWindows.ToArray();
			}

			VisualHelper.InvokeOnUIDispatcher(() =>
			{
				foreach (var hWnd in targets.Where(x => !VirtualDesktopHelper.MoveToDesktop(x, e.NewDesktop)))
				{
					this.helper.MoveWindowToDesktop(hWnd, e.NewDesktop.Id);
				}
			});
		}

		public void Dispose()
		{
			VirtualDesktop.CurrentChanged -= this.VirtualDesktopOnCurrentChanged;
		}
	}
}
