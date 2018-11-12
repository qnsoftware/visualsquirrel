/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.IntellisensePresenter
{
    [Export(typeof(IIntellisensePresenterProvider))]
    [ContentType("nut")]
    [Order(Before = "Default Completion Presenter")]
    [Name("IntellisensePresenterProvider")]
    internal class IntellisensePresenterProvider : IIntellisensePresenterProvider
    {
        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session)
        {
            ICompletionSession completionSession = session as ICompletionSession;
            if (completionSession != null)
            {
                return new CompletionSessionPresenter(ServiceProvider, completionSession);
            }

            return null;
        }
    }
}