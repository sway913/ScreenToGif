﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// The Resizing Adorner controls. https://social.msdn.microsoft.com/Forums/vstudio/en-US/274bc547-dadf-42b5-b3f1-6d29407f9e79/resize-adorner-scale-problem?forum=wpf
    /// </summary>
    public class SelectControl : Control
    {
        #region Variables

        /// <summary>
        /// Resizing adorner uses Thumbs for visual elements.  
        /// The Thumbs have built-in mouse input handling.
        /// </summary>
        private Thumb _topLeft, _topRight, _bottomLeft, _bottomRight, _top, _bottom, _left, _right;

        /// <summary>
        /// The selection rectangle, used to drag the selection Rect elsewhere.
        /// </summary>
        private Rectangle _rectangle;

        /// <summary>
        /// The grid that holds the three buttons to control the selection.
        /// </summary>
        private Grid _statusControlGrid;
        
        /// <summary>
        /// Status control buttons.
        /// </summary>
        private ImageButton _acceptButton, _retryButton, _cancelButton;

        /// <summary>
        /// The start point for the drag operation.
        /// </summary>
        private Point _startPoint;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register("IsPickingRegion", typeof(bool), typeof(SelectControl), new PropertyMetadata(true));

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(Rect), typeof(SelectControl), new PropertyMetadata(new Rect(-1, -1, 0, 0)));

        public static readonly DependencyProperty FinishedSelectionProperty = DependencyProperty.Register("FinishedSelection", typeof(bool), typeof(SelectControl), new PropertyMetadata(false));

        public static readonly RoutedEvent SelectionAcceptedEvent = EventManager.RegisterRoutedEvent("SelectionAccepted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        public static readonly RoutedEvent SelectionCanceledEvent = EventManager.RegisterRoutedEvent("SelectionCanceled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        #endregion

        #region Properties

        public bool IsPickingRegion
        {
            get { return (bool)GetValue(IsPickingRegionProperty); }
            set { SetValue(IsPickingRegionProperty, value); }
        }

        public Rect Selected
        {
            get { return (Rect)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public bool FinishedSelection
        {
            get { return (bool)GetValue(FinishedSelectionProperty); }
            set { SetValue(FinishedSelectionProperty, value); }
        }

        public event RoutedEventHandler SelectionAccepted
        {
            add { AddHandler(SelectionAcceptedEvent, value); }
            remove { RemoveHandler(SelectionAcceptedEvent, value); }
        }

        public event RoutedEventHandler SelectionCanceled
        {
            add { AddHandler(SelectionCanceledEvent, value); }
            remove { RemoveHandler(SelectionCanceledEvent, value); }
        }

        #endregion

        static SelectControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectControl), new FrameworkPropertyMetadata(typeof(SelectControl)));
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _topLeft = Template.FindName("TopLeftThumb", this) as Thumb;
            _topRight = Template.FindName("TopRightThumb", this) as Thumb;
            _bottomLeft = Template.FindName("BottomLeftThumb", this) as Thumb;
            _bottomRight = Template.FindName("BottomRightThumb", this) as Thumb;

            _top = Template.FindName("TopThumb", this) as Thumb;
            _bottom = Template.FindName("BottomThumb", this) as Thumb;
            _left = Template.FindName("LeftThumb", this) as Thumb;
            _right = Template.FindName("RightThumb", this) as Thumb;

            _rectangle = Template.FindName("SelectRectangle", this) as Rectangle;
            _statusControlGrid = Template.FindName("StatusControlGrid", this) as Grid;
            _acceptButton = Template.FindName("AcceptButton", this) as ImageButton;
            _retryButton = Template.FindName("RetryButton", this) as ImageButton;
            _cancelButton = Template.FindName("CancelButton", this) as ImageButton;

            if (_topLeft == null || _topRight == null || _bottomLeft == null || _bottomRight == null ||
                _top == null || _bottom == null || _left == null || _right == null || _rectangle == null)
                return;

            //Add handlers for resizing • Corners.
            _topLeft.DragDelta += HandleTopLeft;
            _topRight.DragDelta += HandleTopRight;
            _bottomLeft.DragDelta += HandleBottomLeft;
            _bottomRight.DragDelta += HandleBottomRight;

            //Add handlers for resizing • Sides.
            _top.DragDelta += HandleTop;
            _bottom.DragDelta += HandleBottom;
            _left.DragDelta += HandleLeft;
            _right.DragDelta += HandleRight;

            //Drag to move.
            _rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
            _rectangle.MouseMove += Rectangle_MouseMove;
            _rectangle.MouseLeftButtonUp += Rectangle_MouseLeftButtonUp;

            if (_acceptButton == null || _retryButton == null || _cancelButton == null)
                return;

            _acceptButton.Click += (sender, e) => { Accept(); };
            _retryButton.Click += (sender, e) => { Retry(); };
            _cancelButton.Click += (sender, e) => { Cancel(); };
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            Selected = new Rect(e.GetPosition(this), new Size(0, 0));

            CaptureMouse();
            HideStatusControls();

            FinishedSelection = false;

            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            Retry();

            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
                return;

            var current = e.GetPosition(this);

            Selected = new Rect(Math.Min(current.X, _startPoint.X), Math.Min(current.Y, _startPoint.Y),
                                Math.Abs(current.X - _startPoint.X), Math.Abs(current.Y - _startPoint.Y));

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();

            if (Selected.Width < 10 || Selected.Height < 10)
            {
                OnMouseRightButtonDown(e);
                return;
            }

            AdjustThumbs();
            ShowStatusControls();

            FinishedSelection = true;

            //e.Handled = true;
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Cancel();

            if (e.Key == Key.Enter)
                Accept();

            e.Handled = true;

            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region Methods

        private void AdjustThumbs()
        {
            //Top left.
            Canvas.SetLeft(_topLeft, Selected.Left - _topLeft.Width / 2);
            Canvas.SetTop(_topLeft, Selected.Top - _topLeft.Height / 2);

            //Top right.
            Canvas.SetLeft(_topRight, Selected.Right - _topRight.Width / 2);
            Canvas.SetTop(_topRight, Selected.Top - _topRight.Height / 2);

            //Bottom left.
            Canvas.SetLeft(_bottomLeft, Selected.Left - _bottomLeft.Width / 2);
            Canvas.SetTop(_bottomLeft, Selected.Bottom - _bottomLeft.Height / 2);

            //Bottom right.
            Canvas.SetLeft(_bottomRight, Selected.Right - _bottomRight.Width / 2);
            Canvas.SetTop(_bottomRight, Selected.Bottom - _bottomRight.Height / 2);

            //Top.
            Canvas.SetLeft(_top, Selected.Left + Selected.Width / 2 - _top.Width / 2);
            Canvas.SetTop(_top, Selected.Top - _top.Height / 2);

            //Left.
            Canvas.SetLeft(_left, Selected.Left - _left.Width / 2);
            Canvas.SetTop(_left, Selected.Top + Selected.Height / 2 - _left.Height / 2);

            //Right.
            Canvas.SetLeft(_right, Selected.Right - _right.Width / 2);
            Canvas.SetTop(_right, Selected.Top + Selected.Height / 2 - _right.Height / 2);

            //Bottom.
            Canvas.SetLeft(_bottom, Selected.Left + Selected.Width / 2 - _bottom.Width / 2);
            Canvas.SetTop(_bottom, Selected.Bottom - _bottom.Height / 2);
        }

        private void ShowStatusControls()
        {
            if (_statusControlGrid == null)
                return;

            if (Selected.Width > 100 && Selected.Height > 100)
            {
                //Show inside the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2  - 50);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - 15);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (ActualHeight - (Selected.Top + Selected.Height) > 100)
            {
                //Show at the bottom of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2 - 50);
                Canvas.SetTop(_statusControlGrid, Selected.Bottom + 10);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (Selected.Top > 100)
            {
                //Show on top of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2 - 50);
                Canvas.SetTop(_statusControlGrid, Selected.Top - 40);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (Selected.Left > 100)
            {
                //Show to the left of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left - 110);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - 15);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (ActualWidth - (Selected.Left + Selected.Width) > 100)
            {
                //Show to the right of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Right + 10);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - 15);

                _statusControlGrid.Visibility = Visibility.Visible;
            }
        }

        private void HideStatusControls()
        {
            if (_statusControlGrid == null)
                return;

            _statusControlGrid.Visibility = Visibility.Collapsed;
        }

        private void Accept()
        {
            if (!FinishedSelection)
                return;

            HideStatusControls();
            RaiseAcceptedEvent();
        }

        public void Retry()
        {
            Selected = new Rect(-1, -1, 0, 0);

            FinishedSelection = false;

            HideStatusControls();
        }

        public void Cancel()
        {
            Selected = new Rect(-1, -1, 0, 0);

            FinishedSelection = false;

            HideStatusControls();
            RaiseCanceledEvent();
        }

        public void RaiseAcceptedEvent()
        {
            if (SelectionAcceptedEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionAcceptedEvent));
        }

        public void RaiseCanceledEvent()
        {
            if (SelectionCanceledEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionCanceledEvent));
        }

        #endregion

        #region Events

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);

            _rectangle.CaptureMouse();

            HideStatusControls();

            e.Handled = true;
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_rectangle.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;

            _rectangle.MouseMove -= Rectangle_MouseMove;

            var currentPosition =  e.GetPosition(this);

            var x = Selected.X + (currentPosition.X - _startPoint.X);
            var y = Selected.Y + (currentPosition.Y - _startPoint.Y);

            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;

            if (x + Selected.Width > ActualWidth)
                x = ActualWidth - Selected.Width;

            if (y + Selected.Height > ActualHeight)
                y = ActualHeight - Selected.Height;

            Selected = new Rect(x, y, Selected.Width, Selected.Height);

            _startPoint = currentPosition;
            e.Handled = true;

            AdjustThumbs();

            _rectangle.MouseMove += Rectangle_MouseMove;
        }

        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_rectangle.IsMouseCaptured)
                _rectangle?.ReleaseMouseCapture();

            AdjustThumbs();
            ShowStatusControls();

            e.Handled = true;
        }

        ///<summary>
        ///Handler for resizing from the top-left.
        ///</summary>
        private void HandleTopLeft(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
            var left = Selected.Left - (width - Selected.Width);
            var height = Math.Max(Selected.Height - e.VerticalChange, 10);
            var top = Selected.Top - (height - Selected.Height);

            if (top < 0)
            {
                height -= top * -1;
                top = 0;
            }

            if (left < 0)
            {
                width -= left * -1;
                left = 0;
            }

            Selected = new Rect(left, top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        ///  Handler for resizing from the top-right.
        /// </summary>
        private void HandleTopRight(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
            var height = Math.Max(Selected.Height - e.VerticalChange, 10);
            var top = Selected.Top - (height - Selected.Height);

            if (top < 0)
            {
                height -= top * -1;
                top = 0;
            }

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            Selected = new Rect(Selected.Left, top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        ///  Handler for resizing from the bottom-left.
        /// </summary>
        private void HandleBottomLeft(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
            var left = Selected.Left - (width - Selected.Width);
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (left < 0)
            {
                width -= left * -1;
                left = 0;
            }

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(left, Selected.Top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the bottom-right.
        /// </summary>
        private void HandleBottomRight(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(Selected.Left, Selected.Top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the left-middle.
        /// </summary>
        private void HandleLeft(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
            var left = Selected.Left - (width - Selected.Width);

            if (left < 0)
            {
                width -= left * -1;
                left = 0;
            }

            Selected = new Rect(left, Selected.Top, width, Selected.Height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the top-middle.
        /// </summary>
        private void HandleTop(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var height = Math.Max(Selected.Height - e.VerticalChange, 10);
            var top = Selected.Top - (height - Selected.Height);

            if (top < 0)
            {
                height -= top * -1;
                top = 0;
            }

            Selected = new Rect(Selected.Left, top, Selected.Width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        ///  Handler for resizing from the right-middle.
        /// </summary>
        private void HandleRight(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            Selected = new Rect(Selected.Left, Selected.Top, width, Selected.Height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the bottom-middle.
        /// </summary>
        private void HandleBottom(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(Selected.Left, Selected.Top, Selected.Width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        #endregion
    }
}