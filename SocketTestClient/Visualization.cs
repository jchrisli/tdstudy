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
using System.Windows.Media.Animation;

namespace SocketTestClient
{
    public class Vis
    {
        protected Canvas parentCanvas;
        protected List<UIElement> elements;
  
        public Vis(Canvas parent)
        {
            this.elements = new List<UIElement>();
        }

        public void AddToParent(Canvas parent)
        {
            this.parentCanvas = parent;
            foreach (UIElement e in this.elements)
            {
                this.parentCanvas.Children.Add(e);
                Canvas.SetLeft(e, -((System.Windows.FrameworkElement)e).Width / 2);
                Canvas.SetLeft(e, -((System.Windows.FrameworkElement)e).Height / 2);
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new ScaleTransform(1, 1));
                tg.Children.Add(new TranslateTransform(0, 0));
                //e.RenderTransform = new ScaleTransform(1, 1);
                e.RenderTransformOrigin = new Point(.5, .5);
                e.RenderTransform = tg;
                Canvas.SetZIndex(e, 1);
            }
        }

        public void DetachFromParent()
        {
            if (parentCanvas != null && this.elements != null)
                foreach(UIElement e in this.elements)
                this.parentCanvas.Children.Remove(e);
        }

        public void ToggleVisibility(bool visible)
        {
            foreach(UIElement e in this.elements)
            {
                e.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            }
  
        }

        public virtual void Resize(double resize)
        {
        }

        public virtual void ToggleColor(bool t)
        {
        }

        public virtual void StartAnimation()
        {
        }

        public virtual void StopAnimation()
        {
        }

        public void Move(double toX, double toY)
        {
            /*foreach (UIElement e in this.elements)
            {
                Transform t = e.RenderTransform;
                Matrix m = t.Value;
                m.OffsetX = toX;
                m.OffsetY = toY;
                e.RenderTransform = new  MatrixTransform(m);
            }*/
            foreach (UIElement e in this.elements)
            {
                ((TransformGroup)e.RenderTransform).Children[1] = new TranslateTransform(toX, toY);
            }
        }
        
    }

    class DotVis : Vis
    {
        public DotVis(Canvas parent) : base(parent)
        {
            Ellipse dotShape = new Ellipse();
            dotShape.Width = 30;
            dotShape.Height = 30;
            dotShape.Fill = new SolidColorBrush(Colors.Beige);
            dotShape.Stroke = new SolidColorBrush(Colors.Transparent);
            this.elements.Add(dotShape);
        }

        public override void Resize(double resize)
        {
            if (resize > 0)
            {
                ((Ellipse)(this.elements[0])).Width = 30 * resize;
                ((Ellipse)(this.elements[0])).Height = 30 * resize;
            }
        }

        public override void ToggleColor(bool t)
        {
            ((Ellipse)(this.elements[0])).Fill = t ? new SolidColorBrush(Colors.Turquoise) : new SolidColorBrush(Colors.Beige);
        }

    }

    class RippleVis : Vis
    {
        DoubleAnimation smallRippleWidthAnimation;
        DoubleAnimation smallRippleHeightAnimation;
        DoubleAnimation largeRippleWidthAnimation;
        DoubleAnimation largeRippleHeightAnimation;
        Storyboard rippleStoryBoard;
        double resize;
        Ellipse small;
        Ellipse large;
        
        public RippleVis(Canvas parent) : base(parent)
        {
            small = new Ellipse();
            small.Fill = new SolidColorBrush(Colors.Transparent);
            small.Stroke = new SolidColorBrush(Colors.Beige);
            small.Width = 20;
            small.Height = 20;
            this.elements.Add(small);

            large = new Ellipse();
            large.Fill = new SolidColorBrush(Colors.Transparent);
            large.Stroke = new SolidColorBrush(Colors.Beige);
            large.Width = 20;
            large.Height = 20;
            this.elements.Add(large);

            this.resize = 1;
            this.smallRippleWidthAnimation = new DoubleAnimation();
            this.smallRippleWidthAnimation.From = 0;
            this.smallRippleWidthAnimation.To = 1;

            this.smallRippleHeightAnimation = new DoubleAnimation();
            this.smallRippleHeightAnimation.From = 0;
            this.smallRippleHeightAnimation.To = 1;

            this.largeRippleWidthAnimation = new DoubleAnimation();
            this.largeRippleWidthAnimation.From = 0;
            this.largeRippleWidthAnimation.To = 2;

            this.largeRippleHeightAnimation = new DoubleAnimation();
            this.largeRippleHeightAnimation.From = 0;
            this.largeRippleHeightAnimation.To = 2;

            this.smallRippleWidthAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            this.smallRippleHeightAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));

            this.largeRippleWidthAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
            this.largeRippleHeightAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));

            this.smallRippleWidthAnimation.BeginTime = TimeSpan.FromMilliseconds(500);
            this.smallRippleHeightAnimation.BeginTime = TimeSpan.FromMilliseconds(500);

            this.smallRippleWidthAnimation.RepeatBehavior = RepeatBehavior.Forever;
            this.largeRippleHeightAnimation.RepeatBehavior = RepeatBehavior.Forever;
            this.smallRippleHeightAnimation.RepeatBehavior = RepeatBehavior.Forever;
            this.largeRippleWidthAnimation.RepeatBehavior = RepeatBehavior.Forever;

            this.smallRippleWidthAnimation.Completed += new EventHandler(smallRippleAnimation_Completed);
            this.largeRippleHeightAnimation.Completed += new EventHandler(largeRippleAnimation_Completed);

            this.rippleStoryBoard = new Storyboard();
            this.rippleStoryBoard.Children.Add(smallRippleWidthAnimation);
            this.rippleStoryBoard.Children.Add(smallRippleHeightAnimation);
            this.rippleStoryBoard.Children.Add(largeRippleWidthAnimation);
            this.rippleStoryBoard.Children.Add(largeRippleHeightAnimation);

            Storyboard.SetTarget(smallRippleWidthAnimation, small);
            Storyboard.SetTarget(smallRippleHeightAnimation, small);
            Storyboard.SetTarget(largeRippleWidthAnimation, large);
            Storyboard.SetTarget(largeRippleHeightAnimation, large);

            Storyboard.SetTargetProperty(smallRippleWidthAnimation, new PropertyPath("RenderTransform.Children[0].ScaleX"));
            Storyboard.SetTargetProperty(smallRippleHeightAnimation, new PropertyPath("RenderTransform.Children[0].ScaleY"));
            Storyboard.SetTargetProperty(largeRippleWidthAnimation, new PropertyPath("RenderTransform.Children[0].ScaleX"));
            Storyboard.SetTargetProperty(largeRippleHeightAnimation, new PropertyPath("RenderTransform.Children[0].ScaleY"));
         
        }

        void  largeRippleAnimation_Completed(object sender, EventArgs e)
        {
            this.largeRippleWidthAnimation.To = this.resize * 2;
            this.largeRippleHeightAnimation.To = this.resize * 2;

        }

        void  smallRippleAnimation_Completed(object sender, EventArgs e)
        {
            this.smallRippleWidthAnimation.To = this.resize;
            this.smallRippleHeightAnimation.To = this.resize;
        }

        public override void StartAnimation()
        {
            this.rippleStoryBoard.Begin();
        }

        public override void StopAnimation()
        {
            this.rippleStoryBoard.Stop();
        }

        public override void Resize(double r)
        {
            if (r < 0)
                this.resize = 0;
            else this.resize = r;
        }

        public override void ToggleColor(bool t)
        {
            foreach (UIElement e in this.elements)
            {
                ((Ellipse)(e)).Stroke = t ? new SolidColorBrush(Colors.DarkTurquoise) : new SolidColorBrush(Colors.Beige);
            }
        }
    }
}

