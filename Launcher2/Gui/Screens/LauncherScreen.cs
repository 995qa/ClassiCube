﻿using System;
using System.Drawing;
using ClassicalSharp;
using OpenTK.Input;

namespace Launcher2 {
	
	public abstract class LauncherScreen {
		protected LauncherWindow game;
		protected IDrawer2D drawer;
		
		public bool Dirty;
		protected int widgetIndex;
		protected bool mouseMoved = false;
		static bool supressMove = true;
		
		public LauncherScreen( LauncherWindow game ) {
			this.game = game;
			drawer = game.Drawer;
		}
		
		public abstract void Init();
		
		/// <summary> Function that is repeatedly called multiple times every second. </summary>
		public abstract void Tick();
		
		public abstract void Resize();
		
		/// <summary> Cleans up all native resources held by this screen. </summary>
		public abstract void Dispose();
		
		protected LauncherWidget selectedWidget;
		protected LauncherWidget[] widgets;
		protected virtual void MouseMove( object sender, MouseMoveEventArgs e ) {
			if( supressMove ) {
				supressMove = false;
				return;
			}
			mouseMoved = true;
			for( int i = 0; i < widgets.Length; i++ ) {
				LauncherWidget widget = widgets[i];
				if( widget == null ) continue;
				if( e.X >= widget.X && e.Y >= widget.Y &&
				   e.X < widget.X + widget.Width && e.Y < widget.Y + widget.Height ) {
					if( selectedWidget == widget ) return;
					
					if( selectedWidget != null )
						UnselectWidget( selectedWidget );
					SelectWidget( widget );
					selectedWidget = widget;
					return;
				}
			}
			
			if( selectedWidget == null ) return;
			UnselectWidget( selectedWidget );
			selectedWidget = null;
		}
		
		protected Font buttonFont;
		
		/// <summary> Called when the user has moved their mouse away from a previously selected widget. </summary>
		protected virtual void UnselectWidget( LauncherWidget widget ) {
			LauncherButtonWidget button = widget as LauncherButtonWidget;
			if( button != null ) {
				button.Active = false;
				button.RedrawBackground();
				using( drawer ) {
					drawer.SetBitmap( game.Framebuffer );
					button.Redraw( drawer );
				}
				Dirty = true;
			}
		}
		
		/// <summary> Called when user has moved their mouse over a given widget. </summary>
		protected virtual void SelectWidget( LauncherWidget widget ) {
			LauncherButtonWidget button = widget as LauncherButtonWidget;
			if( button != null ) {
				button.Active = true;
				button.RedrawBackground();
				using( drawer ) {
					drawer.SetBitmap( game.Framebuffer );
					button.Redraw( drawer );
				}
				Dirty = true;
			}
		}
		
		protected LauncherWidget lastClicked;
		protected void MouseButtonDown( object sender, MouseButtonEventArgs e ) {
			if( e.Button != MouseButton.Left ) return;
			
			if( lastClicked != null && lastClicked != selectedWidget )
				WidgetUnclicked( lastClicked );
			if( selectedWidget != null && selectedWidget.OnClick != null )
				selectedWidget.OnClick( e.X, e.Y );
			lastClicked = selectedWidget;
		}
		
		protected virtual void WidgetUnclicked( LauncherWidget widget ) {
		}
		
		protected bool tabDown = false;
		MouseMoveEventArgs moveArgs = new MouseMoveEventArgs();
		MouseButtonEventArgs pressArgs = new MouseButtonEventArgs();
		protected void HandleTab() {
			if( tabDown ) return;
			tabDown = true;
			int index = lastClicked == null ? -1 :
				Array.IndexOf<LauncherWidget>( widgets, lastClicked );
			int dir = (game.Window.Keyboard[Key.ShiftLeft]
			           || game.Window.Keyboard[Key.ShiftRight]) ? -1 : 1;
			index += dir;
			Utils.Clamp( ref index, 0, widgets.Length - 1);
			
			for( int j = 0; j < widgets.Length * 2; j++ ) {
				int i = (j * dir + index) % widgets.Length;
				if( i < 0 ) i += widgets.Length;
				
				if( widgets[i] is LauncherInputWidget || widgets[i] is LauncherButtonWidget ) {
					LauncherWidget widget = widgets[i];
					moveArgs.X = widget.X + widget.Width / 2;
					moveArgs.Y = widget.Y + widget.Height / 2;
					
					pressArgs.Button = MouseButton.Left;
					pressArgs.IsPressed = true;
					pressArgs.X = moveArgs.X;
					pressArgs.Y = moveArgs.Y;
					
					MouseMove( null, moveArgs );
					Point p = game.Window.PointToScreen( Point.Empty );
					p.Offset( moveArgs.X, moveArgs.Y );
					game.Window.DesktopCursorPos = p;
					lastClicked = widget;
					
					if( widgets[i] is LauncherInputWidget ) {
						MouseButtonDown( null, pressArgs );
						((LauncherInputWidget)widgets[i]).CaretPos = -1;
					}
					break;
				}
			}
		}
		
		protected void MakeButtonAt( string text, int width, int height, Font font,
		                            Anchor verAnchor, int x, int y, Action<int, int> onClick ) {
			MakeButtonAt( text, width, height, font, Anchor.Centre, verAnchor, x, y, onClick );
		}
		
		protected void MakeButtonAt( string text, int width, int height, Font font, Anchor horAnchor,
		                            Anchor verAnchor, int x, int y, Action<int, int> onClick ) {
			LauncherButtonWidget widget;
			if( widgets[widgetIndex] != null ) {
				widget = (LauncherButtonWidget)widgets[widgetIndex];
			} else {
				widget = new LauncherButtonWidget( game );
				widget.Text = text;
				widget.OnClick = onClick;
				widgets[widgetIndex] = widget;
			}
			
			widget.Active = false;
			widget.SetDrawData( drawer, text, font, horAnchor, verAnchor, width, height, x, y );
			widget.Redraw( drawer );
			widgetIndex++;
		}
		
		protected void MakeLabelAt( string text, Font font, Anchor horAnchor, Anchor verAnchor, int x, int y ) {
			LauncherLabelWidget widget;
			if( widgets[widgetIndex] != null ) {
				widget = (LauncherLabelWidget)widgets[widgetIndex];
			} else {
				widget = new LauncherLabelWidget( game, text );
				widgets[widgetIndex] = widget;
			}
			
			widget.SetDrawData( drawer, text, font, horAnchor, verAnchor, x, y );
			widget.Redraw( drawer );
			widgetIndex++;
		}
		
		protected void MakeBooleanAt( Anchor horAnchor, Anchor verAnchor, Font font, bool initValue,
		                             int width, int height, int x, int y, Action<int, int> onClick ) {
			LauncherBooleanWidget widget;
			if( widgets[widgetIndex] != null ) {
				widget = (LauncherBooleanWidget)widgets[widgetIndex];
			} else {
				widget = new LauncherBooleanWidget( game, font, width, height );
				widget.Value = initValue;
				widget.OnClick = onClick;
				widgets[widgetIndex] = widget;
			}
			
			widget.SetDrawData( drawer, horAnchor, verAnchor, x, y );
			widget.Redraw( drawer );
			widgetIndex++;
		}
	}
}
