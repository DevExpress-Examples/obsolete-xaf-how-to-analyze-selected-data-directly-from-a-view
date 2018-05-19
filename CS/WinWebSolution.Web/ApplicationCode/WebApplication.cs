using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp.Web;

namespace WinWebSolution.Web {
    public partial class WinWebSolutionAspNetApplication : WebApplication {
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            args.ObjectSpaceProvider = new XPObjectSpaceProviderThreadSafe(args.ConnectionString, args.Connection);
        }
        private DevExpress.ExpressApp.SystemModule.SystemModule module1;
        private DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule module2;
        private WinWebSolution.Module.WinWebSolutionModule module3;
        private DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule module6;
        private DevExpress.ExpressApp.PivotChart.Web.PivotChartAspNetModule pivotChartAspNetModule1;
        private DevExpress.ExpressApp.PivotChart.PivotChartModuleBase pivotChartModuleBase1;
        private System.Data.SqlClient.SqlConnection sqlConnection1;

        public WinWebSolutionAspNetApplication() {
            InitializeComponent();
        }

        private void WinWebSolutionAspNetApplication_DatabaseVersionMismatch(object sender, DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e) {
            if (System.Diagnostics.Debugger.IsAttached) {
                e.Updater.Update();
                e.Handled = true;
            } else {
                throw new InvalidOperationException(
                    "The application cannot connect to the specified database, because the latter doesn't exist or its version is older than that of the application.\r\n" +
                    "The automatical update is disabled, because the application was started without debugging.\r\n" +
                    "You should start the application under Visual Studio, or modify the " +
                    "source code of the 'DatabaseVersionMismatch' event handler to enable automatic database update, " +
                    "or manually create a database with the help of the 'DBUpdater' tool.");
            }
        }

        private void InitializeComponent() {
            this.module1 = new DevExpress.ExpressApp.SystemModule.SystemModule();
            this.module2 = new DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule();
            this.module3 = new WinWebSolution.Module.WinWebSolutionModule();
            this.module6 = new DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule();
            this.sqlConnection1 = new System.Data.SqlClient.SqlConnection();
            this.pivotChartAspNetModule1 = new DevExpress.ExpressApp.PivotChart.Web.PivotChartAspNetModule();
            this.pivotChartModuleBase1 = new DevExpress.ExpressApp.PivotChart.PivotChartModuleBase();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // module3
            // 
            this.module3.AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.Analysis));
            // 
            // sqlConnection1
            // 
            this.sqlConnection1.ConnectionString = "Data Source=(local);Initial Catalog=WinWebSolution;Integrated Security=SSPI;Pooli" +
                "ng=false";
            this.sqlConnection1.FireInfoMessageEventOnUserErrors = false;
            // 
            // pivotChartAspNetModule1
            // 
            this.pivotChartAspNetModule1.Description = "";
            this.pivotChartAspNetModule1.RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            this.pivotChartAspNetModule1.RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.PivotChart.PivotChartModuleBase));
            // 
            // WinWebSolutionAspNetApplication
            // 
            this.ApplicationName = "WinWebSolution";
            this.Connection = this.sqlConnection1;
            this.Modules.Add(this.module1);
            this.Modules.Add(this.module2);
            this.Modules.Add(this.module6);
            this.Modules.Add(this.module3);
            this.Modules.Add(this.pivotChartModuleBase1);
            this.Modules.Add(this.pivotChartAspNetModule1);
            this.DatabaseVersionMismatch += new System.EventHandler<DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs>(this.WinWebSolutionAspNetApplication_DatabaseVersionMismatch);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
    }
}
