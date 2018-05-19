Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports DevExpress.ExpressApp.Web

Namespace WinWebSolution.Web
	Partial Public Class WinWebSolutionAspNetApplication
		Inherits WebApplication
		Private module1 As DevExpress.ExpressApp.SystemModule.SystemModule
		Private module2 As DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule
		Private module3 As WinWebSolution.Module.WinWebSolutionModule
		Private module6 As DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule
		Private pivotChartAspNetModule1 As DevExpress.ExpressApp.PivotChart.Web.PivotChartAspNetModule
		Private pivotChartModuleBase1 As DevExpress.ExpressApp.PivotChart.PivotChartModuleBase
		Private sqlConnection1 As System.Data.SqlClient.SqlConnection

		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub WinWebSolutionAspNetApplication_DatabaseVersionMismatch(ByVal sender As Object, ByVal e As DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs) Handles MyBase.DatabaseVersionMismatch
			If System.Diagnostics.Debugger.IsAttached Then
				e.Updater.Update()
				e.Handled = True
			Else
				Throw New InvalidOperationException("The application cannot connect to the specified database, because the latter doesn't exist or its version is older than that of the application." & Constants.vbCrLf & "The automatical update is disabled, because the application was started without debugging." & Constants.vbCrLf & "You should start the application under Visual Studio, or modify the " & "source code of the 'DatabaseVersionMismatch' event handler to enable automatic database update, " & "or manually create a database with the help of the 'DBUpdater' tool.")
			End If
		End Sub

		Private Sub InitializeComponent()
			Me.module1 = New DevExpress.ExpressApp.SystemModule.SystemModule()
			Me.module2 = New DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule()
			Me.module3 = New WinWebSolution.Module.WinWebSolutionModule()
			Me.module6 = New DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule()
			Me.sqlConnection1 = New System.Data.SqlClient.SqlConnection()
			Me.pivotChartAspNetModule1 = New DevExpress.ExpressApp.PivotChart.Web.PivotChartAspNetModule()
			Me.pivotChartModuleBase1 = New DevExpress.ExpressApp.PivotChart.PivotChartModuleBase()
			CType(Me, System.ComponentModel.ISupportInitialize).BeginInit()
			' 
			' module3
			' 
			Me.module3.AdditionalBusinessClasses.Add(GetType(DevExpress.Persistent.BaseImpl.Analysis))
			' 
			' sqlConnection1
			' 
			Me.sqlConnection1.ConnectionString = "Data Source=(local);Initial Catalog=WinWebSolution;Integrated Security=SSPI;Pooli" & "ng=false"
			Me.sqlConnection1.FireInfoMessageEventOnUserErrors = False
			' 
			' pivotChartAspNetModule1
			' 
			Me.pivotChartAspNetModule1.Description = ""
			Me.pivotChartAspNetModule1.RequiredModuleTypes.Add(GetType(DevExpress.ExpressApp.SystemModule.SystemModule))
			Me.pivotChartAspNetModule1.RequiredModuleTypes.Add(GetType(DevExpress.ExpressApp.PivotChart.PivotChartModuleBase))
			' 
			' WinWebSolutionAspNetApplication
			' 
			Me.ApplicationName = "WinWebSolution"
			Me.Connection = Me.sqlConnection1
			Me.Modules.Add(Me.module1)
			Me.Modules.Add(Me.module2)
			Me.Modules.Add(Me.module6)
			Me.Modules.Add(Me.module3)
			Me.Modules.Add(Me.pivotChartModuleBase1)
			Me.Modules.Add(Me.pivotChartAspNetModule1)
'			Me.DatabaseVersionMismatch += New System.EventHandler(Of DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs)(Me.WinWebSolutionAspNetApplication_DatabaseVersionMismatch);
			CType(Me, System.ComponentModel.ISupportInitialize).EndInit()

		End Sub
	End Class
End Namespace
