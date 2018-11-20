﻿using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;

namespace Manina.Windows.Forms
{
    public partial class WizardControl
    {
        internal class WizardControlDesigner : ParentControlDesigner
        {
            #region Member Variables
            private BehaviorService behaviorService;
            private ISelectionService selectionService;

            private DesignerVerb addPageVerb;
            private DesignerVerb removePageVerb;
            private DesignerVerb navigateBackVerb;
            private DesignerVerb navigateNextVerb;
            private DesignerVerbCollection verbs;

            private GlyphToolBar toolbar;
            private ButtonGlyph addPageButton;
            private ButtonGlyph removePageButton;
            private ButtonGlyph navigateBackButton;
            private ButtonGlyph navigateNextButton;
            private LabelGlyph currentPageLabel;

            private Adorner toolbarAdorner;
            #endregion

            #region Properties
            public override DesignerVerbCollection Verbs => verbs;

            public new WizardControl Control => (WizardControl)base.Control;
            #endregion

            #region Glyph Icons
            private static PointF[] GetLeftArrowSign(float size)
            {
                float arrowHeadThickness = size;
                float arrowTailThickness = 0.375f * size;
                float arrowHeadLength = 0.5625f * size;
                float arrowTailLength = size - arrowHeadLength;

                return new PointF[] {
                    new PointF(0, size / 2f),
                    new PointF(arrowHeadLength, size / 2f - arrowHeadThickness / 2f),
                    new PointF(arrowHeadLength, size / 2f - arrowTailThickness / 2f),
                    new PointF(arrowHeadLength + arrowTailLength, size / 2f - arrowTailThickness / 2f),
                    new PointF(arrowHeadLength + arrowTailLength, size / 2f + arrowTailThickness / 2f),
                    new PointF(arrowHeadLength, size / 2f + arrowTailThickness / 2f),
                    new PointF(arrowHeadLength, size / 2f + arrowHeadThickness / 2f),
                };
            }

            private static PointF[] GetRightArrowSign(float size)
            {
                float arrowHeadThickness = size;
                float arrowTailThickness = 0.375f * size;
                float arrowHeadLength = 0.5625f * size;
                float arrowTailLength = size - arrowHeadLength;

                return new PointF[] {
                    new PointF(size, size / 2f),
                    new PointF(size - arrowHeadLength, size / 2f - arrowHeadThickness / 2f),
                    new PointF(size - arrowHeadLength, size / 2f - arrowTailThickness / 2f),
                    new PointF(size - arrowHeadLength - arrowTailLength, size / 2f - arrowTailThickness / 2f),
                    new PointF(size - arrowHeadLength - arrowTailLength, size / 2f + arrowTailThickness / 2f),
                    new PointF(size - arrowHeadLength, size / 2f + arrowTailThickness / 2f),
                    new PointF(size - arrowHeadLength, size / 2f + arrowHeadThickness / 2f),
                };
            }

            private static PointF[] GetPlusSign(float size)
            {
                float thickness = 0.375f * size;

                return new PointF[] {
                    new PointF(0, size / 2f - thickness / 2f),
                    new PointF(size / 2f - thickness / 2f, size / 2f - thickness / 2f),
                    new PointF(size / 2f - thickness / 2f, 0),
                    new PointF(size / 2f + thickness / 2f, 0),
                    new PointF(size / 2f + thickness / 2f, size / 2f - thickness / 2f),
                    new PointF(size, size / 2f - thickness / 2f),
                    new PointF(size, size / 2f + thickness / 2f),
                    new PointF(size / 2f + thickness / 2f, size / 2f + thickness / 2f),
                    new PointF(size / 2f + thickness / 2f, size),
                    new PointF(size / 2f - thickness / 2f, size),
                    new PointF(size / 2f - thickness / 2f, size / 2f + thickness / 2f),
                    new PointF(0, size / 2f + thickness / 2f),
                };
            }

            private static PointF[] GetMinusSign(float size)
            {
                float thickness = 0.375f * size;

                return new PointF[] {
                    new PointF(0, size / 2f - thickness / 2f),
                    new PointF(size, size / 2f - thickness / 2f),
                    new PointF(size, size / 2f + thickness / 2f),
                    new PointF(0, size / 2f + thickness / 2f),
                };
            }
            #endregion

            #region Initialize/Dispose
            public override void Initialize(IComponent component)
            {
                base.Initialize(component);

                navigateBackVerb = new DesignerVerb("Previous page", new EventHandler(NavigateBackHandler));
                navigateNextVerb = new DesignerVerb("Next page", new EventHandler(NavigateNextHandler));
                addPageVerb = new DesignerVerb("Add page", new EventHandler(AddPageHandler));
                removePageVerb = new DesignerVerb("Remove page", new EventHandler(RemovePageHandler));

                verbs = new DesignerVerbCollection();
                verbs.AddRange(new DesignerVerb[] { navigateBackVerb, navigateNextVerb, addPageVerb, removePageVerb });

                behaviorService = (BehaviorService)GetService(typeof(BehaviorService));
                selectionService = (ISelectionService)GetService(typeof(ISelectionService));

                CreateGlyphs();

                Control.PageChanged += Control_CurrentPageChanged;
                Control.ControlAdded += Control_ControlAdded;
                Control.ControlRemoved += Control_ControlRemoved;
                Control.Resize += Control_Resize;
            }

            public override void InitializeNewComponent(IDictionary defaultValues)
            {
                base.InitializeNewComponent(defaultValues);

                // add a default page
                AddPageHandler(this, EventArgs.Empty);

                MemberDescriptor member = TypeDescriptor.GetProperties(Component)["Controls"];
                RaiseComponentChanging(member);
                RaiseComponentChanged(member, null, null);

                Control.SelectedIndex = 0;

                UpdateGlyphs();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Control.PageChanged -= Control_CurrentPageChanged;
                    Control.ControlAdded -= Control_ControlAdded;
                    Control.ControlRemoved -= Control_ControlRemoved;
                    Control.Resize -= Control_Resize;

                    navigateBackButton.Click -= NavigateBackButton_Click;
                    navigateNextButton.Click -= NavigateNextButton_Click;
                    addPageButton.Click -= AddPageButton_Click;
                    removePageButton.Click -= RemovePageButton_Click;

                    if (behaviorService != null)
                        behaviorService.Adorners.Remove(toolbarAdorner);
                }
                base.Dispose(disposing);
            }
            #endregion

            #region Helper Methods
            /// <summary>
            /// Creates the glyphs for navigation and manipulating pages
            /// </summary>
            private void CreateGlyphs()
            {
                toolbarAdorner = new Adorner();
                behaviorService.Adorners.Add(toolbarAdorner);

                toolbar = new GlyphToolBar(behaviorService, this, toolbarAdorner);

                navigateBackButton = new ButtonGlyph();
                navigateBackButton.Path = GetLeftArrowSign(toolbar.DefaultIconSize.Height);

                navigateNextButton = new ButtonGlyph();
                navigateNextButton.Path = GetRightArrowSign(toolbar.DefaultIconSize.Height);

                addPageButton = new ButtonGlyph();
                addPageButton.Path = GetPlusSign(toolbar.DefaultIconSize.Height);

                removePageButton = new ButtonGlyph();
                removePageButton.Path = GetMinusSign(toolbar.DefaultIconSize.Height);

                currentPageLabel = new LabelGlyph();
                currentPageLabel.Text = string.Format("Page {0} of {1}", Control.SelectedIndex + 1, Control.Pages.Count);

                navigateBackButton.Click += NavigateBackButton_Click;
                navigateNextButton.Click += NavigateNextButton_Click;
                addPageButton.Click += AddPageButton_Click;
                removePageButton.Click += RemovePageButton_Click;

                toolbar.AddButton(navigateBackButton);
                toolbar.AddButton(currentPageLabel);
                toolbar.AddButton(navigateNextButton);
                toolbar.AddButton(new SeparatorGlyph());
                toolbar.AddButton(addPageButton);
                toolbar.AddButton(removePageButton);

                toolbarAdorner.Glyphs.Add(toolbar);
            }

            private void Control_CurrentPageChanged(object sender, WizardControl.PageChangedEventArgs e)
            {
                UpdateGlyphs();
            }

            private void Control_ControlAdded(object sender, ControlEventArgs e)
            {
                UpdateGlyphs();
            }

            private void Control_ControlRemoved(object sender, ControlEventArgs e)
            {
                UpdateGlyphs();
            }

            private void Control_Resize(object sender, EventArgs e)
            {
                toolbar.UpdateLayout();
                toolbar.Location = new Point(8, Control.UIArea.Top + (Control.UIArea.Height - toolbar.Size.Height) / 2);
            }

            private void NavigateBackButton_Click(object sender, EventArgs e)
            {
                NavigateBackHandler(this, EventArgs.Empty);
            }

            private void NavigateNextButton_Click(object sender, EventArgs e)
            {
                NavigateNextHandler(this, EventArgs.Empty);
            }

            private void AddPageButton_Click(object sender, EventArgs e)
            {
                AddPageHandler(this, EventArgs.Empty);
            }

            private void RemovePageButton_Click(object sender, EventArgs e)
            {
                RemovePageHandler(this, EventArgs.Empty);
            }

            /// <summary>
            /// Gets the designer of the current page.
            /// </summary>
            /// <returns>The designer of the wizard page currently active in the designer.</returns>
            private WizardPage.WizardPageDesigner GetCurrentPageDesigner()
            {
                var page = Control.SelectedPage;
                if (page != null)
                {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    if (host != null)
                        return (WizardPage.WizardPageDesigner)host.GetDesigner(page);
                }
                return null;
            }

            /// <summary>
            /// Updates the visual states of glyphs.
            /// </summary>
            private void UpdateGlyphs()
            {
                removePageVerb.Enabled = removePageButton.Enabled = (Control.Pages.Count > 1);
                navigateBackVerb.Enabled = navigateBackButton.Enabled = (Control.SelectedIndex > 0);
                navigateNextVerb.Enabled = navigateNextButton.Enabled = (Control.SelectedIndex < Control.Pages.Count - 1);
                currentPageLabel.Text = string.Format("Page {0} of {1}", Control.SelectedIndex + 1, Control.Pages.Count);

                toolbarAdorner.Invalidate();
            }
            #endregion

            #region Verb Handlers
            /// <summary>
            /// Adds a new wizard page.
            /// </summary>
            protected void AddPageHandler(object sender, EventArgs e)
            {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                if (host != null)
                {
                    WizardPage page = (WizardPage)host.CreateComponent(typeof(WizardPage));
                    Control.Pages.Add(page);
                    Control.SelectedPage = page;

                    selectionService.SetSelectedComponents(new Component[] { Control.SelectedPage });
                }
            }

            /// <summary>
            /// Removes the current wizard page.
            /// </summary>
            protected void RemovePageHandler(object sender, EventArgs e)
            {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                if (host != null)
                {
                    if (Control.Pages.Count > 1)
                    {
                        WizardPage page = Control.SelectedPage;
                        if (page != null)
                        {
                            int index = Control.SelectedIndex;
                            //Control.Pages.Remove(page);
                            host.DestroyComponent(page);
                            if (index == Control.Pages.Count)
                                index = Control.Pages.Count - 1;
                            Control.SelectedIndex = index;

                            selectionService.SetSelectedComponents(new Component[] { Control.SelectedPage });
                        }
                    }
                }
            }

            /// <summary>
            /// Navigates to the previous wizard page.
            /// </summary>
            protected void NavigateBackHandler(object sender, EventArgs e)
            {
                WizardControl control = Control;

                if (control.CanGoBack)
                    control.GoBack();

                selectionService.SetSelectedComponents(new Component[] { Control.SelectedPage });
            }

            /// <summary>
            /// Navigates to the next wizard page.
            /// </summary>
            protected void NavigateNextHandler(object sender, EventArgs e)
            {
                WizardControl control = Control;

                if (control.CanGoNext)
                    control.GoNext();

                selectionService.SetSelectedComponents(new Component[] { Control.SelectedPage });
            }
            #endregion

            #region Delegate All Drag Events To The Current Page
            protected override void OnDragEnter(DragEventArgs de)
            {
                GetCurrentPageDesigner().OnDragEnter(de);
            }

            protected override void OnDragOver(DragEventArgs de)
            {
                Point pt = Control.PointToClient(new Point(de.X, de.Y));

                if (!Control.DisplayRectangle.Contains(pt))
                    de.Effect = DragDropEffects.None;
                else
                    GetCurrentPageDesigner().OnDragOver(de);
            }

            protected override void OnDragLeave(EventArgs e)
            {
                GetCurrentPageDesigner().OnDragLeave(e);
            }

            protected override void OnDragDrop(DragEventArgs de)
            {
                GetCurrentPageDesigner().OnDragDrop(de);
            }

            protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
            {
                GetCurrentPageDesigner().OnGiveFeedback(e);
            }

            protected override void OnDragComplete(DragEventArgs de)
            {
                GetCurrentPageDesigner().OnDragComplete(de);
            }
            #endregion
        }
    }
}
