﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

namespace Microsoft.DwayneNeed.Threading
{
    /// <summary>
    ///     The VisualTargetPresentationSource represents the root
    ///     of a visual subtree owned by a different thread that the
    ///     visual tree in which is is displayed.
    /// </summary>
    /// <remarks>
    ///     A HostVisual belongs to the same UI thread that owns the
    ///     visual tree in which it resides.
    ///     
    ///     A HostVisual can reference a VisualTarget owned by another
    ///     thread.
    ///     
    ///     A VisualTarget has a root visual.
    ///     
    ///     VisualTargetPresentationSource wraps the VisualTarget and
    ///     enables basic functionality like Loaded, which depends on
    ///     a PresentationSource being available.
    /// </remarks>
    public class VisualTargetPresentationSource : PresentationSource
    {
        public VisualTargetPresentationSource(HostVisual hostVisual)
        {
            _visualTarget = new VisualTarget(hostVisual);
        }

        public override Visual RootVisual
        {
            get
            {
                return _visualTarget.RootVisual;
            }

            set
            {
                Visual oldRoot = _visualTarget.RootVisual;


                // Set the root visual of the VisualTarget.  This visual will
                // now be used to visually compose the scene.
                _visualTarget.RootVisual = value;

                // Hook the SizeChanged event on framework elements for all
                // future changed to the layout size of our root, and manually
                // trigger a size change.
                FrameworkElement rootFE = value as FrameworkElement;
                if (rootFE != null)
                {
                    rootFE.SizeChanged += new SizeChangedEventHandler(root_SizeChanged);
                    rootFE.DataContext = _dataContext;
                    
                    // HACK!
                    if (_propertyName != null)
                    {
                        Binding myBinding = new Binding(_propertyName);
                        myBinding.Source = _dataContext;
                        rootFE.SetBinding(TextBlock.TextProperty, myBinding);
                    }
                }

                // Tell the PresentationSource that the root visual has
                // changed.  This kicks off a bunch of stuff like the
                // Loaded event.
                RootChanged(oldRoot, value);

                // Kickoff layout...
                UIElement rootElement = value as UIElement;
                if (rootElement != null)
                {
                    rootElement.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    rootElement.Arrange(new Rect(rootElement.DesiredSize));
                }
            }
        }

        public object DataContext
        {
            get {return _dataContext;}
            set
            {
                _dataContext = value;
                var rootElement = _visualTarget.RootVisual as FrameworkElement;
                if (rootElement != null)
                {
                    rootElement.DataContext = _dataContext;
                }
            }
        }

        // HACK!
        public string PropertyName
        {
            get { return _propertyName; }
            set
            {
                _propertyName = value;

                var rootElement = _visualTarget.RootVisual as TextBlock;
                if (rootElement != null)
                {
                    if (!rootElement.CheckAccess())
                    {
                        throw new InvalidOperationException("What?");
                    }

                    Binding myBinding = new Binding(_propertyName);
                    myBinding.Source = _dataContext;
                    rootElement.SetBinding(TextBlock.TextProperty, myBinding);
                }
            }
        }

        public event SizeChangedEventHandler SizeChanged;

        public override bool IsDisposed
        {
            get
            {
                // We don't support disposing this object.
                return false;
            }
        }

        protected override CompositionTarget GetCompositionTargetCore()
        {
            return _visualTarget;
        }

        private void root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChangedEventHandler handler = SizeChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private VisualTarget _visualTarget;
        private object _dataContext;
        private string _propertyName;
    }
}
