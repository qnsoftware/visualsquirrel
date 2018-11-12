/* see LICENSE notice in solution root */

using System;

using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;

namespace Squirrel.SquirrelLanguageService
{


	public class SquirrelColorableItem : ColorableItem
    {

		private string displayName;
		private COLORINDEX background;
		private COLORINDEX foreground;

		public SquirrelColorableItem(string displayName, COLORINDEX foreground, COLORINDEX background)
            : base(displayName, displayName, foreground, background, System.Drawing.Color.Gray, System.Drawing.Color.Black, FONTFLAGS.FF_DEFAULT)
		{
			this.displayName = displayName;
			this.background = background;
			this.foreground = foreground;
		}

		#region IVsColorableItem Members

		public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground)
		{
			if (null == piForeground)
			{
				throw new ArgumentNullException("piForeground");
			}
			if (0 == piForeground.Length)
			{
				throw new ArgumentOutOfRangeException("piForeground");
			}
			piForeground[0] = foreground;

			if (null == piBackground)
			{
				throw new ArgumentNullException("piBackground");
			}
			if (0 == piBackground.Length)
			{
				throw new ArgumentOutOfRangeException("piBackground");
			}
			piBackground[0] = background;

			return Microsoft.VisualStudio.VSConstants.S_OK;
		}

		public int GetDefaultFontFlags(out uint pdwFontFlags)
		{
			pdwFontFlags = 0;
			return Microsoft.VisualStudio.VSConstants.S_OK;
		}
        public override int GetCanonicalName(out string name)
        {
            name = displayName;
            return Microsoft.VisualStudio.VSConstants.S_OK;
            // return base.GetCanonicalName(out name);
        }
        public int GetDisplayName(out string pbstrName)
		{
			pbstrName = displayName;
			return Microsoft.VisualStudio.VSConstants.S_OK;
		}
        public override int GetMergingPriority(out int priority)
        {
            priority = 0x2000;
            return Microsoft.VisualStudio.VSConstants.S_OK;
            //return base.GetMergingPriority(out priority);
        }
        #endregion
    }


}
