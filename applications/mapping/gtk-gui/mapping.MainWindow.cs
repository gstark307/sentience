// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.42
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace mapping {
    
    
    public partial class MainWindow {
        
        private Gtk.Action File;
        
        private Gtk.Action Tools;
        
        private Gtk.Action Simulation;
        
        private Gtk.Action File1;
        
        private Gtk.Action Exit1;
        
        private Gtk.VBox vbox2;
        
        private Gtk.MenuBar menubar1;
        
        private Gtk.HBox hbox2;
        
        private Gtk.VBox vbox1;
        
        private Gtk.Label label1;
        
        private Gtk.Label label2;
        
        private Gtk.Label label3;
        
        private Gtk.Label label4;
        
        private Gtk.VBox vbox3;
        
        private Gtk.TextView txtSimulationName;
        
        private Gtk.TextView txtRobotDesignFile;
        
        private Gtk.TextView txtStereoImagesPath;
        
        private Gtk.TextView txtTuningParameters;
        
        private Gtk.VBox vbox4;
        
        private Gtk.Button cmdRobotDesignFileBrowse;
        
        private Gtk.Button cmdStereoImagesPathBrowse;
        
        private Gtk.HBox hbox1;
        
        private Gtk.VBox vbox5;
        
        private Gtk.VPaned vpanedPathSegments;
        
        private Gtk.HBox hbox4;
        
        private Gtk.VBox vbox6;
        
        private Gtk.Label label5;
        
        private Gtk.Label label6;
        
        private Gtk.Label label7;
        
        private Gtk.VBox vbox7;
        
        private Gtk.TextView txtXPosition;
        
        private Gtk.TextView txtYPosition;
        
        private Gtk.TextView txtHeading;
        
        private Gtk.VBox vbox8;
        
        private Gtk.Label label8;
        
        private Gtk.Label label9;
        
        private Gtk.Label label10;
        
        private Gtk.VBox vbox9;
        
        private Gtk.TextView txtNoOfSteps;
        
        private Gtk.TextView txtDistancePerStep;
        
        private Gtk.TextView txtHeadingChangePerStep;
        
        private Gtk.HBox hbox3;
        
        private Gtk.Button cmdAdd;
        
        private Gtk.Button cmdRemove;
        
        private Gtk.Image imgRobotPath;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize(this);
            // Widget mapping.MainWindow
            Gtk.UIManager w1 = new Gtk.UIManager();
            Gtk.ActionGroup w2 = new Gtk.ActionGroup("Default");
            this.File = new Gtk.Action("File", Mono.Unix.Catalog.GetString("File"), null, null);
            this.File.ShortLabel = Mono.Unix.Catalog.GetString("File");
            w2.Add(this.File, null);
            this.Tools = new Gtk.Action("Tools", Mono.Unix.Catalog.GetString("Tools"), null, null);
            this.Tools.ShortLabel = Mono.Unix.Catalog.GetString("Tools");
            w2.Add(this.Tools, null);
            this.Simulation = new Gtk.Action("Simulation", Mono.Unix.Catalog.GetString("Simulation"), null, null);
            this.Simulation.ShortLabel = Mono.Unix.Catalog.GetString("Simulation");
            w2.Add(this.Simulation, null);
            this.File1 = new Gtk.Action("File1", Mono.Unix.Catalog.GetString("File"), null, null);
            this.File1.ShortLabel = Mono.Unix.Catalog.GetString("File");
            w2.Add(this.File1, null);
            this.Exit1 = new Gtk.Action("Exit1", Mono.Unix.Catalog.GetString("Exit"), null, null);
            this.Exit1.ShortLabel = Mono.Unix.Catalog.GetString("Exit");
            w2.Add(this.Exit1, null);
            w1.InsertActionGroup(w2, 0);
            this.AddAccelGroup(w1.AccelGroup);
            this.Name = "mapping.MainWindow";
            this.Title = Mono.Unix.Catalog.GetString("Sentience Mapping");
            this.WindowPosition = ((Gtk.WindowPosition)(4));
            // Container child mapping.MainWindow.Gtk.Container+ContainerChild
            this.vbox2 = new Gtk.VBox();
            this.vbox2.Name = "vbox2";
            this.vbox2.Spacing = 6;
            // Container child vbox2.Gtk.Box+BoxChild
            w1.AddUiFromString("<ui><menubar name='menubar1'><menu action='File1'><menuitem action='Exit1'/></menu><menu action='Tools'><menuitem action='Simulation'/></menu></menubar></ui>");
            this.menubar1 = ((Gtk.MenuBar)(w1.GetWidget("/menubar1")));
            this.menubar1.Name = "menubar1";
            this.vbox2.Add(this.menubar1);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.vbox2[this.menubar1]));
            w3.Position = 0;
            w3.Expand = false;
            w3.Fill = false;
            // Container child vbox2.Gtk.Box+BoxChild
            this.hbox2 = new Gtk.HBox();
            this.hbox2.Name = "hbox2";
            this.hbox2.Spacing = 6;
            // Container child hbox2.Gtk.Box+BoxChild
            this.vbox1 = new Gtk.VBox();
            this.vbox1.Name = "vbox1";
            this.vbox1.Spacing = 6;
            // Container child vbox1.Gtk.Box+BoxChild
            this.label1 = new Gtk.Label();
            this.label1.Name = "label1";
            this.label1.LabelProp = Mono.Unix.Catalog.GetString("Simulation Name");
            this.vbox1.Add(this.label1);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(this.vbox1[this.label1]));
            w4.Position = 0;
            w4.Expand = false;
            w4.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.label2 = new Gtk.Label();
            this.label2.Name = "label2";
            this.label2.LabelProp = Mono.Unix.Catalog.GetString("Robot Design File");
            this.vbox1.Add(this.label2);
            Gtk.Box.BoxChild w5 = ((Gtk.Box.BoxChild)(this.vbox1[this.label2]));
            w5.Position = 1;
            w5.Expand = false;
            w5.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.label3 = new Gtk.Label();
            this.label3.Name = "label3";
            this.label3.LabelProp = Mono.Unix.Catalog.GetString("Stereo Images path");
            this.vbox1.Add(this.label3);
            Gtk.Box.BoxChild w6 = ((Gtk.Box.BoxChild)(this.vbox1[this.label3]));
            w6.Position = 2;
            w6.Expand = false;
            w6.Fill = false;
            // Container child vbox1.Gtk.Box+BoxChild
            this.label4 = new Gtk.Label();
            this.label4.Name = "label4";
            this.label4.LabelProp = Mono.Unix.Catalog.GetString("Tuning Parameters");
            this.vbox1.Add(this.label4);
            Gtk.Box.BoxChild w7 = ((Gtk.Box.BoxChild)(this.vbox1[this.label4]));
            w7.Position = 3;
            w7.Expand = false;
            w7.Fill = false;
            this.hbox2.Add(this.vbox1);
            Gtk.Box.BoxChild w8 = ((Gtk.Box.BoxChild)(this.hbox2[this.vbox1]));
            w8.Position = 0;
            w8.Expand = false;
            w8.Fill = false;
            // Container child hbox2.Gtk.Box+BoxChild
            this.vbox3 = new Gtk.VBox();
            this.vbox3.Name = "vbox3";
            this.vbox3.Spacing = 6;
            // Container child vbox3.Gtk.Box+BoxChild
            this.txtSimulationName = new Gtk.TextView();
            this.txtSimulationName.CanFocus = true;
            this.txtSimulationName.Name = "txtSimulationName";
            this.vbox3.Add(this.txtSimulationName);
            Gtk.Box.BoxChild w9 = ((Gtk.Box.BoxChild)(this.vbox3[this.txtSimulationName]));
            w9.Position = 0;
            // Container child vbox3.Gtk.Box+BoxChild
            this.txtRobotDesignFile = new Gtk.TextView();
            this.txtRobotDesignFile.CanFocus = true;
            this.txtRobotDesignFile.Name = "txtRobotDesignFile";
            this.vbox3.Add(this.txtRobotDesignFile);
            Gtk.Box.BoxChild w10 = ((Gtk.Box.BoxChild)(this.vbox3[this.txtRobotDesignFile]));
            w10.Position = 1;
            // Container child vbox3.Gtk.Box+BoxChild
            this.txtStereoImagesPath = new Gtk.TextView();
            this.txtStereoImagesPath.CanFocus = true;
            this.txtStereoImagesPath.Name = "txtStereoImagesPath";
            this.vbox3.Add(this.txtStereoImagesPath);
            Gtk.Box.BoxChild w11 = ((Gtk.Box.BoxChild)(this.vbox3[this.txtStereoImagesPath]));
            w11.Position = 2;
            // Container child vbox3.Gtk.Box+BoxChild
            this.txtTuningParameters = new Gtk.TextView();
            this.txtTuningParameters.CanFocus = true;
            this.txtTuningParameters.Name = "txtTuningParameters";
            this.vbox3.Add(this.txtTuningParameters);
            Gtk.Box.BoxChild w12 = ((Gtk.Box.BoxChild)(this.vbox3[this.txtTuningParameters]));
            w12.Position = 3;
            this.hbox2.Add(this.vbox3);
            Gtk.Box.BoxChild w13 = ((Gtk.Box.BoxChild)(this.hbox2[this.vbox3]));
            w13.Position = 1;
            // Container child hbox2.Gtk.Box+BoxChild
            this.vbox4 = new Gtk.VBox();
            this.vbox4.Name = "vbox4";
            this.vbox4.Spacing = 6;
            // Container child vbox4.Gtk.Box+BoxChild
            this.cmdRobotDesignFileBrowse = new Gtk.Button();
            this.cmdRobotDesignFileBrowse.CanFocus = true;
            this.cmdRobotDesignFileBrowse.Name = "cmdRobotDesignFileBrowse";
            this.cmdRobotDesignFileBrowse.UseUnderline = true;
            this.cmdRobotDesignFileBrowse.Label = Mono.Unix.Catalog.GetString("Browse");
            this.vbox4.Add(this.cmdRobotDesignFileBrowse);
            Gtk.Box.BoxChild w14 = ((Gtk.Box.BoxChild)(this.vbox4[this.cmdRobotDesignFileBrowse]));
            w14.Position = 1;
            w14.Expand = false;
            w14.Fill = false;
            // Container child vbox4.Gtk.Box+BoxChild
            this.cmdStereoImagesPathBrowse = new Gtk.Button();
            this.cmdStereoImagesPathBrowse.CanFocus = true;
            this.cmdStereoImagesPathBrowse.Name = "cmdStereoImagesPathBrowse";
            this.cmdStereoImagesPathBrowse.UseUnderline = true;
            this.cmdStereoImagesPathBrowse.Label = Mono.Unix.Catalog.GetString("Browse");
            this.vbox4.Add(this.cmdStereoImagesPathBrowse);
            Gtk.Box.BoxChild w15 = ((Gtk.Box.BoxChild)(this.vbox4[this.cmdStereoImagesPathBrowse]));
            w15.Position = 2;
            w15.Expand = false;
            w15.Fill = false;
            this.hbox2.Add(this.vbox4);
            Gtk.Box.BoxChild w16 = ((Gtk.Box.BoxChild)(this.hbox2[this.vbox4]));
            w16.Position = 2;
            w16.Expand = false;
            w16.Fill = false;
            this.vbox2.Add(this.hbox2);
            Gtk.Box.BoxChild w17 = ((Gtk.Box.BoxChild)(this.vbox2[this.hbox2]));
            w17.Position = 1;
            w17.Expand = false;
            w17.Fill = false;
            // Container child vbox2.Gtk.Box+BoxChild
            this.hbox1 = new Gtk.HBox();
            this.hbox1.Name = "hbox1";
            this.hbox1.Spacing = 6;
            // Container child hbox1.Gtk.Box+BoxChild
            this.vbox5 = new Gtk.VBox();
            this.vbox5.Name = "vbox5";
            this.vbox5.Spacing = 6;
            // Container child vbox5.Gtk.Box+BoxChild
            this.vpanedPathSegments = new Gtk.VPaned();
            this.vpanedPathSegments.CanFocus = true;
            this.vpanedPathSegments.Name = "vpanedPathSegments";
            this.vpanedPathSegments.Position = 10;
            this.vbox5.Add(this.vpanedPathSegments);
            Gtk.Box.BoxChild w18 = ((Gtk.Box.BoxChild)(this.vbox5[this.vpanedPathSegments]));
            w18.Position = 0;
            // Container child vbox5.Gtk.Box+BoxChild
            this.hbox4 = new Gtk.HBox();
            this.hbox4.Name = "hbox4";
            this.hbox4.Spacing = 6;
            // Container child hbox4.Gtk.Box+BoxChild
            this.vbox6 = new Gtk.VBox();
            this.vbox6.Name = "vbox6";
            this.vbox6.Spacing = 6;
            // Container child vbox6.Gtk.Box+BoxChild
            this.label5 = new Gtk.Label();
            this.label5.Name = "label5";
            this.label5.LabelProp = Mono.Unix.Catalog.GetString("X position mm");
            this.vbox6.Add(this.label5);
            Gtk.Box.BoxChild w19 = ((Gtk.Box.BoxChild)(this.vbox6[this.label5]));
            w19.Position = 0;
            w19.Expand = false;
            w19.Fill = false;
            // Container child vbox6.Gtk.Box+BoxChild
            this.label6 = new Gtk.Label();
            this.label6.Name = "label6";
            this.label6.LabelProp = Mono.Unix.Catalog.GetString("Y position mm");
            this.vbox6.Add(this.label6);
            Gtk.Box.BoxChild w20 = ((Gtk.Box.BoxChild)(this.vbox6[this.label6]));
            w20.Position = 1;
            w20.Expand = false;
            w20.Fill = false;
            // Container child vbox6.Gtk.Box+BoxChild
            this.label7 = new Gtk.Label();
            this.label7.Name = "label7";
            this.label7.LabelProp = Mono.Unix.Catalog.GetString("Heading degrees");
            this.vbox6.Add(this.label7);
            Gtk.Box.BoxChild w21 = ((Gtk.Box.BoxChild)(this.vbox6[this.label7]));
            w21.Position = 2;
            w21.Expand = false;
            w21.Fill = false;
            this.hbox4.Add(this.vbox6);
            Gtk.Box.BoxChild w22 = ((Gtk.Box.BoxChild)(this.hbox4[this.vbox6]));
            w22.Position = 0;
            w22.Expand = false;
            w22.Fill = false;
            // Container child hbox4.Gtk.Box+BoxChild
            this.vbox7 = new Gtk.VBox();
            this.vbox7.Name = "vbox7";
            this.vbox7.Spacing = 6;
            // Container child vbox7.Gtk.Box+BoxChild
            this.txtXPosition = new Gtk.TextView();
            this.txtXPosition.Buffer.Text = "0000";
            this.txtXPosition.CanFocus = true;
            this.txtXPosition.Name = "txtXPosition";
            this.vbox7.Add(this.txtXPosition);
            Gtk.Box.BoxChild w23 = ((Gtk.Box.BoxChild)(this.vbox7[this.txtXPosition]));
            w23.Position = 0;
            // Container child vbox7.Gtk.Box+BoxChild
            this.txtYPosition = new Gtk.TextView();
            this.txtYPosition.Buffer.Text = "0000";
            this.txtYPosition.CanFocus = true;
            this.txtYPosition.Name = "txtYPosition";
            this.vbox7.Add(this.txtYPosition);
            Gtk.Box.BoxChild w24 = ((Gtk.Box.BoxChild)(this.vbox7[this.txtYPosition]));
            w24.Position = 1;
            // Container child vbox7.Gtk.Box+BoxChild
            this.txtHeading = new Gtk.TextView();
            this.txtHeading.Buffer.Text = "0000";
            this.txtHeading.CanFocus = true;
            this.txtHeading.Name = "txtHeading";
            this.vbox7.Add(this.txtHeading);
            Gtk.Box.BoxChild w25 = ((Gtk.Box.BoxChild)(this.vbox7[this.txtHeading]));
            w25.Position = 2;
            this.hbox4.Add(this.vbox7);
            Gtk.Box.BoxChild w26 = ((Gtk.Box.BoxChild)(this.hbox4[this.vbox7]));
            w26.Position = 1;
            // Container child hbox4.Gtk.Box+BoxChild
            this.vbox8 = new Gtk.VBox();
            this.vbox8.Name = "vbox8";
            this.vbox8.Spacing = 6;
            // Container child vbox8.Gtk.Box+BoxChild
            this.label8 = new Gtk.Label();
            this.label8.Name = "label8";
            this.label8.LabelProp = Mono.Unix.Catalog.GetString("Number of steps");
            this.vbox8.Add(this.label8);
            Gtk.Box.BoxChild w27 = ((Gtk.Box.BoxChild)(this.vbox8[this.label8]));
            w27.Position = 0;
            w27.Expand = false;
            w27.Fill = false;
            // Container child vbox8.Gtk.Box+BoxChild
            this.label9 = new Gtk.Label();
            this.label9.Name = "label9";
            this.label9.LabelProp = Mono.Unix.Catalog.GetString("Distance per step mm");
            this.vbox8.Add(this.label9);
            Gtk.Box.BoxChild w28 = ((Gtk.Box.BoxChild)(this.vbox8[this.label9]));
            w28.Position = 1;
            w28.Expand = false;
            w28.Fill = false;
            // Container child vbox8.Gtk.Box+BoxChild
            this.label10 = new Gtk.Label();
            this.label10.Name = "label10";
            this.label10.LabelProp = Mono.Unix.Catalog.GetString("Heading change per step");
            this.vbox8.Add(this.label10);
            Gtk.Box.BoxChild w29 = ((Gtk.Box.BoxChild)(this.vbox8[this.label10]));
            w29.Position = 2;
            w29.Expand = false;
            w29.Fill = false;
            this.hbox4.Add(this.vbox8);
            Gtk.Box.BoxChild w30 = ((Gtk.Box.BoxChild)(this.hbox4[this.vbox8]));
            w30.Position = 2;
            w30.Expand = false;
            w30.Fill = false;
            // Container child hbox4.Gtk.Box+BoxChild
            this.vbox9 = new Gtk.VBox();
            this.vbox9.Name = "vbox9";
            this.vbox9.Spacing = 6;
            // Container child vbox9.Gtk.Box+BoxChild
            this.txtNoOfSteps = new Gtk.TextView();
            this.txtNoOfSteps.Buffer.Text = "0000";
            this.txtNoOfSteps.CanFocus = true;
            this.txtNoOfSteps.Name = "txtNoOfSteps";
            this.vbox9.Add(this.txtNoOfSteps);
            Gtk.Box.BoxChild w31 = ((Gtk.Box.BoxChild)(this.vbox9[this.txtNoOfSteps]));
            w31.Position = 0;
            // Container child vbox9.Gtk.Box+BoxChild
            this.txtDistancePerStep = new Gtk.TextView();
            this.txtDistancePerStep.Buffer.Text = "0000";
            this.txtDistancePerStep.CanFocus = true;
            this.txtDistancePerStep.Name = "txtDistancePerStep";
            this.vbox9.Add(this.txtDistancePerStep);
            Gtk.Box.BoxChild w32 = ((Gtk.Box.BoxChild)(this.vbox9[this.txtDistancePerStep]));
            w32.Position = 1;
            // Container child vbox9.Gtk.Box+BoxChild
            this.txtHeadingChangePerStep = new Gtk.TextView();
            this.txtHeadingChangePerStep.Buffer.Text = "0000";
            this.txtHeadingChangePerStep.CanFocus = true;
            this.txtHeadingChangePerStep.Name = "txtHeadingChangePerStep";
            this.vbox9.Add(this.txtHeadingChangePerStep);
            Gtk.Box.BoxChild w33 = ((Gtk.Box.BoxChild)(this.vbox9[this.txtHeadingChangePerStep]));
            w33.Position = 2;
            this.hbox4.Add(this.vbox9);
            Gtk.Box.BoxChild w34 = ((Gtk.Box.BoxChild)(this.hbox4[this.vbox9]));
            w34.Position = 3;
            this.vbox5.Add(this.hbox4);
            Gtk.Box.BoxChild w35 = ((Gtk.Box.BoxChild)(this.vbox5[this.hbox4]));
            w35.Position = 1;
            w35.Expand = false;
            w35.Fill = false;
            // Container child vbox5.Gtk.Box+BoxChild
            this.hbox3 = new Gtk.HBox();
            this.hbox3.Name = "hbox3";
            this.hbox3.Spacing = 6;
            // Container child hbox3.Gtk.Box+BoxChild
            this.cmdAdd = new Gtk.Button();
            this.cmdAdd.CanFocus = true;
            this.cmdAdd.Name = "cmdAdd";
            this.cmdAdd.UseUnderline = true;
            this.cmdAdd.Label = Mono.Unix.Catalog.GetString("Add");
            this.hbox3.Add(this.cmdAdd);
            Gtk.Box.BoxChild w36 = ((Gtk.Box.BoxChild)(this.hbox3[this.cmdAdd]));
            w36.Position = 0;
            w36.Expand = false;
            w36.Fill = false;
            // Container child hbox3.Gtk.Box+BoxChild
            this.cmdRemove = new Gtk.Button();
            this.cmdRemove.CanFocus = true;
            this.cmdRemove.Name = "cmdRemove";
            this.cmdRemove.UseUnderline = true;
            this.cmdRemove.Label = Mono.Unix.Catalog.GetString("Remove");
            this.hbox3.Add(this.cmdRemove);
            Gtk.Box.BoxChild w37 = ((Gtk.Box.BoxChild)(this.hbox3[this.cmdRemove]));
            w37.Position = 1;
            w37.Expand = false;
            w37.Fill = false;
            this.vbox5.Add(this.hbox3);
            Gtk.Box.BoxChild w38 = ((Gtk.Box.BoxChild)(this.vbox5[this.hbox3]));
            w38.Position = 2;
            w38.Expand = false;
            w38.Fill = false;
            this.hbox1.Add(this.vbox5);
            Gtk.Box.BoxChild w39 = ((Gtk.Box.BoxChild)(this.hbox1[this.vbox5]));
            w39.Position = 0;
            w39.Expand = false;
            w39.Fill = false;
            // Container child hbox1.Gtk.Box+BoxChild
            this.imgRobotPath = new Gtk.Image();
            this.imgRobotPath.Name = "imgRobotPath";
            this.hbox1.Add(this.imgRobotPath);
            Gtk.Box.BoxChild w40 = ((Gtk.Box.BoxChild)(this.hbox1[this.imgRobotPath]));
            w40.Position = 1;
            w40.Expand = false;
            w40.Fill = false;
            this.vbox2.Add(this.hbox1);
            Gtk.Box.BoxChild w41 = ((Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
            w41.Position = 2;
            w41.Expand = false;
            w41.Fill = false;
            this.Add(this.vbox2);
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 793;
            this.DefaultHeight = 485;
            this.Show();
            this.DeleteEvent += new Gtk.DeleteEventHandler(this.OnDeleteEvent);
            this.Simulation.Activated += new System.EventHandler(this.mnuSimulation);
            this.Exit1.Activated += new System.EventHandler(this.OnExit1Activated);
        }
    }
}
