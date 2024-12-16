//#define USEOPENGLSURFACE

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Envitia.MapLink;
using Envitia.MapLink.InteractionModes;
using Envitia.MapLink.OpenGLSurface;

namespace DrawingSurfacePanel
{
#if USEOPENGLSURFACE
  using DrawingSurfaceClass = TSLNOpenGLSurface;
#else
  using DrawingSurfaceClass = TSLNDrawingSurface;
#endif

  /// <summary>
  /// Summary description for drawingSurfacePanel.
  /// </summary>
  public class MapViewerPanel : Panel, IPanel, DrawLineInterationMode.IObserver
  {
    public IPanel GetIPanel() { return this; }

    #region Panel_Fields
    #region Panel_InteractionMode_Parameters
    public enum InteractionModeEnum
    {
      InValid = -1,

      TOOLS_GRAB,
      TOOLS_DRAW_LINE
    }

    struct ContextMenuTool
    {
      public TSLNInteractionMode interactionMode;
      public InteractionModeEnum mode;
      public string text;
      public bool ischecked;
    }

    private ContextMenuTool[] ContextMenuTools { get; set; }

    private InteractionModeEnum CurrentInteractionMode { get; set; }

    //Create out request receiver pointer
    private InteractionModeRequestReceiver UpdateReceiver { get; set; } = null;

    //Create interaction modes' pointers
    private TSLNInteractionModeManagerGeneric ModeManager { get; set; } = null;

    // The interaction mode to draw a line on the map.
    public DrawLineInterationMode DrawLineInterationMode { get; set; } = new DrawLineInterationMode((int)InteractionModeEnum.TOOLS_DRAW_LINE);

    public TSLNStandardDataLayer GeometryLayer { get; } = new TSLNStandardDataLayer();

    #endregion

    //Create the usual MapLink Item pointers
    public DrawingSurfaceClass DrawingSurface { get; set; }

    // context menu when right click is clicked
    ContextMenuStrip ContextMenu { get; set; } = null;

    public event rightClickCurrentModeChangeHandler RightClickCurrentModeChange;
    public delegate void rightClickCurrentModeChangeHandler();

    #endregion

    #region Panel_Contructors

    public MapViewerPanel()
    {
      InitializeComponent();
      //! Set Maplink Home directory here ...
      string maplHome = TSLNUtilityFunctions.getMapLinkHome();
      if (string.IsNullOrEmpty(maplHome))
      {
        TSLNUtilityFunctions.setMapLinkHome("../");
      }

      DrawLineInterationMode.Observers.Add(this);

      //! Initialise drawing surface
      Envitia.MapLink.TSLNDrawingSurface.loadStandardConfig();
      
      InitializePanel();
    }

    ~MapViewerPanel()
    {
      CleanUp();
    }
    #endregion

    #region Panel_Controls

    private MapViewerParentPanel ViewerPanel;

    private void InitializeComponent()
    {
      //! define the panel's controls
      this.ViewerPanel = new MapViewerParentPanel();
      this.SuspendLayout();

      // 
      // ViewerPanel
      // 
      this.ViewerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
      this.ViewerPanel.Location = new System.Drawing.Point(0, 35);
      this.ViewerPanel.Name = "ViewerPanel";
      this.ViewerPanel.Size = new System.Drawing.Size(783, 495);
      this.ViewerPanel.TabIndex = 2;
      //! override interaction functions
      this.ViewerPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
      this.ViewerPanel.Resize += new System.EventHandler(this.OnReSize);
      this.ViewerPanel.HandleDestroyed += new System.EventHandler(this.OnDestroyed);
      this.ViewerPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
      this.ViewerPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
      this.ViewerPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseUp);
      this.ViewerPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.OnMouseWheel);

      //! Add controls to the panel
      this.Controls.Add(this.ViewerPanel);
      this.ResumeLayout(false);
    }
    #endregion

    #region Panel_Basic_Functions
    //Paint of the ViewerPanel
    private void OnPaint(object sender, System.Windows.Forms.PaintEventArgs e)
    {
      const int leftOffset = 0;
      const int bottomOffset = 0;

      if (DrawingSurface != null)
      {
        DrawingSurface.drawDU(e.ClipRectangle.Left + leftOffset, e.ClipRectangle.Top, e.ClipRectangle.Right, e.ClipRectangle.Bottom - bottomOffset, false, false);
      }

      // Don't forget to draw any echo rectangle that may be active.
      if (ModeManager != null)
      {
        ModeManager.onDraw(e.ClipRectangle.Left + leftOffset, e.ClipRectangle.Top, e.ClipRectangle.Right, e.ClipRectangle.Bottom - bottomOffset);
      }
    }

    //Resize of the ViewerPanel
    private void OnReSize(object sender, System.EventArgs e)
    {
      if (DrawingSurface != null)
      {
        DrawingSurface.wndResize(ViewerPanel.DisplayRectangle.Left, ViewerPanel.DisplayRectangle.Top, ViewerPanel.DisplayRectangle.Right, ViewerPanel.DisplayRectangle.Bottom, false, TSLNResizeActionEnum.TSLNResizeActionMaintainCentre);
      }
      if (ModeManager != null)
      {
        ModeManager.onSize(ViewerPanel.DisplayRectangle.Width, ViewerPanel.DisplayRectangle.Height);
      }

      // Without the invalidate, the paint is only called when the window gets bigger
      ViewerPanel.Invalidate();
    }

    //On mouse down on the ViewerPanel
    private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      if (ModeManager == null)
      {
        return;
      }

      bool invalidate = false;

      switch (e.Button)
      {
        case MouseButtons.Left:
          {
            invalidate = ModeManager.onLButtonDown(e.X, e.Y, false, false);
            break;
          }
        case MouseButtons.Middle:
          {
            invalidate = ModeManager.onMButtonDown(e.X, e.Y, false, false);
            break;
          }
        case MouseButtons.Right:
          {
            invalidate = ModeManager.onRButtonDown(e.X, e.Y, false, false);
            break;
          }
      }
      if (invalidate)
      {
        ViewerPanel.Invalidate();
      }
    }
    //On mouse move on the ViewerPanel
    private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      if (ModeManager == null)
      {
        return;
      }
      TSLNButtonType button = TSLNButtonType.TSLNButtonNone;

      if (e.Button == MouseButtons.Left)
        button = TSLNButtonType.TSLNButtonLeft;
      else if (e.Button == MouseButtons.Middle)
        button = TSLNButtonType.TSLNButtonCentre;
      else if (e.Button == MouseButtons.Right)
        button = TSLNButtonType.TSLNButtonRight;

      if (ModeManager.onMouseMove(button, e.X, e.Y, false, false))
      {
        // Request a redraw if the interaction hander requires it
        ViewerPanel.Invalidate();
      }
    }

    //On mouse up on the ViewerPanel
    private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      if (ModeManager == null)
      {
        return;
      }

      bool invalidate = false;
      switch (e.Button)
      {
        case MouseButtons.Left:
          {
            invalidate = ModeManager.onLButtonUp(e.X, e.Y, false, false);
            break;
          }
        case MouseButtons.Middle:
          {
            invalidate = ModeManager.onMButtonUp(e.X, e.Y, false, false);
            break;
          }
        case MouseButtons.Right:
          {
            ShowRClickContextMenu(e);
            break;
          }
      }

      if (invalidate)
      {
        ViewerPanel.Invalidate();
      }
    }

    //On mouse wheel on the ViewerPanel
    private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
    {
      if (ModeManager == null)
      {
        return;
      }
      if (ModeManager.onMouseWheel((short)e.Delta, e.X, e.Y))
      {
        ViewerPanel.Invalidate();
      }
    }

    private void OnDestroyed(object sender, System.EventArgs e)
    {
      CleanUp();
    }

    /// <summary>
    /// initialize the context menu by setting the options
    /// </summary>
    private void InitializeRClickContextMenu()
    {
      if (ContextMenu != null)
        return;

      ContextMenu = new ContextMenuStrip();
      for (int idx = 0; idx < ContextMenuTools.Length; ++idx)
      {
        ContextMenu.Items.Add(ContextMenuTools[idx].text);
        ((ToolStripMenuItem)ContextMenu.Items[idx]).Checked = ContextMenuTools[idx].ischecked;
      }
    }

    /// <summary>
    /// when a check menu item is chosen, it unchecks other options
    /// </summary>
    /// <param name="text"></param>
    /// <param name="contexMenuuu"></param>
    /// <param name="tools"></param>
    private void UpdateContextMenuToolChecked(InteractionModeEnum mode, ContextMenuStrip contexMenuuu, ContextMenuTool[] tools)
    {
      if (contexMenuuu == null)
        return;

      for (int idx = 0; idx < ContextMenuTools.Length; ++idx)
      {
        ((ToolStripMenuItem)contexMenuuu.Items[idx]).Checked = ContextMenuTools[idx].mode == mode;
      }
    }

    private void UpdateRightClickModeChangeEvent()
    {
      if (this.RightClickCurrentModeChange != null)
      {
        this.RightClickCurrentModeChange();
      }
    }

    /// <summary>
    /// show right click context menu
    /// </summary>
    /// <param name="e"></param>
    private void ShowRClickContextMenu(System.Windows.Forms.MouseEventArgs e)
    {
      if (ContextMenu == null)
        return;

      ContextMenu.Show(PointToScreen(e.Location));
      ContextMenu.ItemClicked += new ToolStripItemClickedEventHandler(
          ContexMenuuu_ItemClicked);
    }

    /// <summary>
    /// handles when an option is chosen from the right click context menu.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContexMenuuu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
    {
      ToolStripItem item = e.ClickedItem;
      // your code here
      switch (item.Text)
      {
        case "Grab Tool":
          if (CurrentInteractionMode != InteractionModeEnum.TOOLS_GRAB)
          {
            SetCurrentMode(InteractionModeEnum.TOOLS_GRAB);
            UpdateRightClickModeChangeEvent();
          }
          break;

        case "Draw Slice":
          if (CurrentInteractionMode != InteractionModeEnum.TOOLS_DRAW_LINE)
          {
            SetCurrentMode(InteractionModeEnum.TOOLS_DRAW_LINE);
            UpdateRightClickModeChangeEvent();
          }
          break;
      }

    }

    #endregion

    #region Panel_Functions
    public void InitializePanel()
    {
      bool status = DrawingSurfaceClass.loadStandardConfig();
#if USEOPENGLSURFACE
          TSLNOpenGLSurfaceCreationParameters creationOptions = new TSLNOpenGLSurfaceCreationParameters();
          creationOptions.createCoreProfile(true);
          creationOptions.enableDoubleBuffering(true);
          DrawingSurface = new TSLNWGLSurface(ViewerPanel.Handle, false, creationOptions);      
#else
      DrawingSurface = new DrawingSurfaceClass((IntPtr)ViewerPanel.Handle, false);
      DrawingSurface.setOption(TSLNOptionEnum.TSLNOptionDoubleBuffered, true);
      DrawingSurface.setOption(TSLNOptionEnum.TSLNOptionAntiAliasMonoRasters, true);
#endif

      UpdateReceiver = new InteractionModeRequestReceiver();
      // Attach the drawing surface to the form and set it's initial size...
      // This must be done before the drawingsurface is given to the
      // InteractionModeManager
      DrawingSurface.attach(ViewerPanel.Handle, false);

      // Create our user defined request receiver
      TSLNInteractionModeRequest request = (TSLNInteractionModeRequest)UpdateReceiver;

      // Now create the mode manager
      ModeManager = new TSLNInteractionModeManagerGeneric(request, DrawingSurface, 5, 5, 30, true);

      // Next create our modes
      InitializeInteractionModes();

      //Give them to the interaction mode manager
      //Remember we don't own the modes anymore after this point
      //but we must keep a reference to them or c# won't deleted them properly
      foreach (var tool in ContextMenuTools)
      {
        ModeManager.addMode(tool.interactionMode, tool.ischecked);
      }

      DrawingSurface.wndResize(ViewerPanel.DisplayRectangle.Left, ViewerPanel.DisplayRectangle.Top, ViewerPanel.DisplayRectangle.Right, ViewerPanel.DisplayRectangle.Bottom, false, TSLNResizeActionEnum.TSLNResizeActionMaintainCentre);
      ModeManager.onSize(ViewerPanel.DisplayRectangle.Width, ViewerPanel.DisplayRectangle.Height);

      DrawingSurface.setOption(TSLNOptionEnum.TSLNOptionDoubleBuffered, true);
      
      //activate grab imode
      SetCurrentMode(InteractionModeEnum.TOOLS_GRAB);

      InitializeRClickContextMenu();

      DrawingSurface.reset(false);
    }

    private void Clear()
    {
      // Clear any data layers currently on the drawing surface
      while (DrawingSurface.numDataLayers > 0)
      {
        TSLNDataLayer dataLayer = null;
        string layerName;
        DrawingSurface.getDataLayerInfo(0, out dataLayer, out layerName);
        DrawingSurface.removeDataLayer(layerName);
      }
    }

    public DrawingSurfacePanel.IMapLayer FoundationLayer { get; private set; }

    public void SetFoundationLayer(DrawingSurfacePanel.IMapLayer foundationLayer)
    {
      Clear();

      FoundationLayer = foundationLayer;

      DrawingSurface.addDataLayer(foundationLayer.GetDataLayer(), foundationLayer.Identifier());
      DrawingSurface.setCoordinateProvidingLayer(foundationLayer.Identifier());
      foundationLayer.ConfigureMapLayer(DrawingSurface);

      EnsureInteractionLayersVisible();
      DrawingSurface.reset();
    }

    private void EnsureVisible(Envitia.MapLink.TSLNDataLayer dataLayer, string id)
    {
      var overlay = DrawingSurface.getDataLayer(id);
      if (overlay == null)
      {
        DrawingSurface.addDataLayer(dataLayer, id);
        overlay = dataLayer;
      }

      DrawingSurface.bringToFront(id);
      DrawingSurface.setDataLayerProps(id, TSLNPropertyEnum.TSLNPropertyVisible, 1);
    }

    public void EnsureInteractionLayersVisible()
    {
      EnsureVisible(GeometryLayer, "Geometry");
      EnsureVisible(DrawLineInterationMode.Overlay, "DrawLineInterationMode.Overlay");
    }  

    private void InitializeInteractionModes()
    {
      ContextMenuTools = new ContextMenuTool[]
      {
        new ContextMenuTool { mode = InteractionModeEnum.TOOLS_GRAB, text = "Grab Tool", ischecked = false},
        new ContextMenuTool { mode = InteractionModeEnum.TOOLS_DRAW_LINE, text = "Draw Slice", ischecked = false}
      };

      for (int i = 0; i < ContextMenuTools.Length; ++i)
      {
        switch (ContextMenuTools[i].text)
        {
          case "Grab Tool":
            ContextMenuTools[i].interactionMode = new TSLNInteractionModeGrab((int)InteractionModeEnum.TOOLS_GRAB, true, "Left button drag move view, Right button click to finish", true);
            break;
          case "Draw Slice":
            ContextMenuTools[i].interactionMode = DrawLineInterationMode;
            break;
        }
      }
    }

    private void CleanUp()
    {
      DrawingSurfaceClass.cleanup();
      DrawingSurface.Dispose();
      DrawingSurface = null;
    }
    

    [DllImport("gdi32.dll")]
    private static extern bool
    BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    #endregion

    #region Panel_ZoomPan_Functions

    public bool SetCurrentMode(InteractionModeEnum mode)
    {
      if (ModeManager == null)
      {
        return false;
      }

      bool invalidate = false;
      for (int i = 0; i < ContextMenuTools.Length; ++i)
      {
        if (mode == ContextMenuTools[i].mode)
        {
          invalidate = ModeManager.setCurrentMode((int)mode);
          break;
        }
      }

      if (invalidate)
      {
        CurrentInteractionMode = mode;
        UpdateContextMenuToolChecked(mode, ContextMenu, ContextMenuTools);
        ViewerPanel.Invalidate();
      }

      return invalidate;
    }

    public InteractionModeEnum GetCurrentMode()
    {
      if (ModeManager == null)
      {
        return InteractionModeEnum.InValid;
      }

      TSLNInteractionMode cmode = null;
      long id = ModeManager.getCurrentMode(out cmode);
      foreach (var tool in ContextMenuTools)
      {
        if (id == (int)tool.mode)
        {
          return tool.mode;
        }
      }

      return InteractionModeEnum.InValid;
    }

    #endregion

    #region Panel_StackViews_Functions
    public bool SaveView(int index)
    {
      if (ModeManager == null)
      {
        return false;
      }
      bool invalidate = ModeManager.savedViewSetToCurrent(index);
      return invalidate;
    }

    public bool ViewStackReset()
    {
      if (ModeManager == null)
      {
        return false;
      }
      bool invalidate = ModeManager.savedViewReset();
      return invalidate;
    }

    public bool GotoSavedView(int index)
    {
      if (ModeManager == null)
      {
        return false;
      }
      bool invalidate = ModeManager.savedViewGoto(index);
      if (invalidate)
      {
        ViewerPanel.Invalidate();
      }
      return invalidate;
    }

    public bool ViewStackGotoPrevious()
    {
      if (ModeManager == null)
      {
        return false;
      }
      bool invalidate = ModeManager.viewStackGotoPrevious();
      if (invalidate)
      {
        ViewerPanel.Invalidate();
      }
      return invalidate;
    }

    public bool ViewStackGotoNext()
    {
      if (ModeManager == null)
      {
        return false;
      }
      bool invalidate = ModeManager.viewStackGotoNext();
      if (invalidate)
      {
        ViewerPanel.Invalidate();
      }
      return invalidate;
    }

    void DrawLineInterationMode.IObserver.Invalidate()
    {
      ViewerPanel.Invalidate();
      base.Invalidate();
    }

    void DrawLineInterationMode.IObserver.NewLine(Tuple<double, double> startMu, Tuple<double, double> endMu)
    {
      EnsureInteractionLayersVisible();
    }

    #endregion

    public void SafeInvalidate()
    {
      if (this.InvokeRequired)
      {
        Action safeInvoke = delegate { SafeInvalidate(); };
        this.Invoke(safeInvoke);
      }
      else
      {
        Invalidate(true);
      }
    }

    TSLN2DDrawingSurface IPanel.GetDrawingSurface()
    {
      return DrawingSurface;
    }
  }
}
