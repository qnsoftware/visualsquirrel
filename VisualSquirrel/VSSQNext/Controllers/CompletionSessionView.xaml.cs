/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using VisualSquirrel.Controllers;
using VisualSquirrel;
using VisualSquirrel.LanguageService;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.IntellisensePresenter
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CompletionSessionView : UserControl
    {
        private CompletionSessionPresenter presenter;

        internal CompletionSessionView(CompletionSessionPresenter presenter)
        {
            InitializeComponent();

            this.presenter = presenter;

            SubscribeToEvents();
            this.DataContext = presenter;
        }

        private void SubscribeToEvents()
        {
            this.presenter.Session.Dismissed += new EventHandler(OnSessionDismissed);
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
            SurrenderFocus();
        }

        private void UnsubscribeFromEvents()
        {
            this.presenter.Session.Dismissed -= new EventHandler(OnSessionDismissed);
        }

        private void SurrenderFocus()
        {
            IWpfTextView view = this.presenter.Session.TextView as IWpfTextView;
            if (view != null)
            {
                Keyboard.Focus(view.VisualElement);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.presenter.SelectedCompletion = this.listViewCompletions.SelectedItem as SQCompletion;
            this.SurrenderFocus();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.presenter.Commit();
        }

        internal void Select(Completion completion)
        {
            this.listViewCompletions.SelectedItem = completion;
            //this.listViewCompletions.Focus();
            if (completion != null)
            {
                this.listViewCompletions.ScrollIntoView(completion);
            }
        }

        private void listViewCompletions_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SurrenderFocus();
        }

        private void OnThumbDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double heightAdjust = this.Height + e.VerticalChange;
            if (heightAdjust >= this.MinHeight)
            {
                this.Height = heightAdjust;
            }

            double widthAdjust = this.Width + e.HorizontalChange;
            if (widthAdjust >= this.MinWidth)
            {
                this.Width = widthAdjust;
            }
        }

        private void OnMsdnImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            presenter.Navigate(@"http://msdn.microsoft.com");
        }

        private void url_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock block = sender as TextBlock;
            SQDeclaration d = block.Tag as SQDeclaration;
            if(d!=null)
                SQVSUtils.OpenDocumentInNewWindow(d.Url, presenter.serviceProvider, d.Span.iStartLine);
            /*if(Dispatcher.CheckAccess())
            {
                SQVSUtils.OpenDocumentInNewWindow(d.Url, presenter.serviceProvider, d.Span.iStartLine);
            }
            else
            {
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    SQVSUtils.OpenDocumentInNewWindow(d.Url, presenter.serviceProvider, d.Span.iStartLine);
                });
            }*/
        }
    }
}