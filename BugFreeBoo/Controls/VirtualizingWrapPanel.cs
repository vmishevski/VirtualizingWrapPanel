using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Controls
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
            "ItemWidth", typeof (double), typeof (VirtualizingWrapPanel), new PropertyMetadata(default(double)));

        public double ItemWidth
        {
            get { return (double) GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            "ItemHeight", typeof (double), typeof (VirtualizingWrapPanel), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ScrollUnitLengthProperty = DependencyProperty.Register(
            "ScrollUnitLength", typeof (double), typeof (VirtualizingWrapPanel), new PropertyMetadata((double)500));

        public double ScrollUnitLength
        {
            get { return (double) GetValue(ScrollUnitLengthProperty); }
            set { SetValue(ScrollUnitLengthProperty, value); }
        }

        private Size _extent;
        private Size _viewport;
        private Point _offset;

        public double ItemHeight
        {
            get { return (double) GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        private TranslateTransform _translate;

        public VirtualizingWrapPanel()
        {
            _translate = new TranslateTransform(0,0);
            this.RenderTransform = _translate;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            
            var itemsPerRow = ItemsPerRow;
            var extent = new Size(availableSize.Width,
                Math.Ceiling((double) ItemsCount/itemsPerRow)*ItemHeight);
            Debug.WriteLine("Extend size: " + extent);

            if (extent != _extent)
            {
                _extent = extent;
                if (ScrollOwner != null)
                    ScrollOwner.InvalidateScrollInfo();
            }

            if (availableSize != _viewport)
            {
                _viewport = availableSize;
                if (ScrollOwner != null)
                    ScrollOwner.InvalidateScrollInfo();
            }

            int firstVisibleIndex;
            int lastVisibleIndex;
            GetVisibleIndexes(out firstVisibleIndex, out lastVisibleIndex);

            var generator = ItemContainerGenerator;

            var firstItemPosition = generator.GeneratorPositionFromIndex(0);
            using (generator.StartAt(firstItemPosition, GeneratorDirection.Forward))
            {
                for (int i = 0; i < ItemsCount; i++)
                {
                    bool isNewRealized;
                    var child = (UIElement)generator.GenerateNext(out isNewRealized);
                    if (isNewRealized)
                    {
                        AddInternalChild(child);
                        generator.PrepareItemContainer(child);
                    }
                    
                }
            }

            if (Children.Count > 0)
            {
                

                var childSize = new Size(ItemWidth, ItemHeight);
                foreach (UIElement child in InternalChildren)
                {
                    child.Measure(childSize);
                }
            }
            

            return extent;
        }

        private void GetVisibleIndexes(out int firstVisibleIndex, out int lastVisibleIndex)
        {
            if (InternalChildren.Count == 0)
            {
                firstVisibleIndex = -1;
                lastVisibleIndex = -1;
            }
            else
            {
                var itemsPerRow = ItemsPerRow;
                firstVisibleIndex = (int)Math.Floor(VerticalOffset / ItemHeight) * itemsPerRow;
                lastVisibleIndex = Math.Max((int)Math.Ceiling((VerticalOffset + ViewportHeight) / ItemHeight) * itemsPerRow - 1, 0);
            }

            Contract.Ensures(lastVisibleIndex >= firstVisibleIndex);
        }

        protected override Size ArrangeOverride(Size finalSize)
        { 
            var extent = new Size(finalSize.Width,
                Math.Ceiling((double)ItemsCount / ItemsPerRow) * ItemHeight);
            Debug.WriteLine("Extend size: " + extent);

            if (extent != _extent)
            {
                _extent = extent;
                if (ScrollOwner != null)
                    ScrollOwner.InvalidateScrollInfo();
            }

            if (finalSize != _viewport)
            {
                _viewport = finalSize;
                if (ScrollOwner != null)
                    ScrollOwner.InvalidateScrollInfo();
            }

            

            var itemsPerRow = ItemsPerRow;
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                var itemRow = GetItemRow(i);

                child.Arrange(new Rect((i%itemsPerRow)*ItemWidth, (itemRow-1)*ItemHeight, ItemWidth, ItemHeight));
            }
            return finalSize;
        }

        private int ItemsCount
        {
            get
            {
                var itemsControl = ItemsControl.GetItemsOwner(this);
                return itemsControl.Items.Count;
            }
        }

        public ItemsControl ItemsControl { get; set; }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - ScrollUnitLength);
        }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + ScrollUnitLength);
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset + ScrollUnitLength);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset - ScrollUnitLength);
        }

        public void PageUp()
        {
            throw new System.NotImplementedException();
        }

        public void PageDown()
        {
            throw new System.NotImplementedException();
        }

        public void PageLeft()
        {
            throw new System.NotImplementedException();
        }

        public void PageRight()
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - ScrollUnitLength);
            if (ScrollOwner != null)
            {
                ScrollOwner.InvalidateScrollInfo();
            }
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + ScrollUnitLength);
            if (ScrollOwner != null)
            {
                ScrollOwner.InvalidateScrollInfo();
            }
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset + ScrollUnitLength);
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset - ScrollUnitLength);
        }

        public void SetHorizontalOffset(double offset)
        {
            throw new System.NotImplementedException();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0)
            {
                offset = 0;
            }
            else if(offset + ViewportHeight >= ExtentHeight)
            {
                offset = ExtentHeight - ViewportHeight;
            }
            _offset.Y = offset;

            _translate.Y = -offset;

            InvalidateMeasure();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            // Need to find the child and ensure that it is visible
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (visual.Equals(child))
                {
                    // found the child. Lets chech if it is visible
                    var itemRow = GetItemRow(i);
                    if (_offset.Y > (itemRow-1) * ItemHeight || _offset.Y+_viewport.Height < (itemRow-1) * ItemHeight)
                    {
                        SetVerticalOffset((itemRow-1) * ItemHeight);
                    }
                    else
                    {
                        // child is visible, do nothing
                    }
                }
            }

            return rectangle;
        }

        private double GetItemRow(int itemIndex)
        {
            var itemsPerRow = ItemsPerRow;
            var itemRow = Math.Ceiling((double) (itemIndex + 1)/itemsPerRow);
            return itemRow;
        }

        private int ItemsPerRow
        {
            get { return (int) Math.Max(1, Math.Floor(ViewportWidth/ItemWidth)); }
        }

        public bool CanVerticallyScroll { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth
        {
            get { return _extent.Width; }
        }

        public double ExtentHeight
        {
            get { return _extent.Height; }
        }

        public double ViewportWidth
        {
            get { return _viewport.Width; }
        }

        public double ViewportHeight
        {
            get { return _viewport.Height; }
        }

        public double HorizontalOffset
        {
            get { return _offset.X; }
        }

        public double VerticalOffset
        {
            get { return _offset.Y; }
        }

        public ScrollViewer ScrollOwner { get; set; }
    }
}
