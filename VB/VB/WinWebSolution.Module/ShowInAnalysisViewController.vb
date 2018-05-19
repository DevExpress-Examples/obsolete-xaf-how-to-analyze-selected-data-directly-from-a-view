Imports Microsoft.VisualBasic
Imports System
Imports DevExpress.Xpo
Imports System.Collections
Imports DevExpress.ExpressApp
Imports DevExpress.Data.Filtering
Imports System.Collections.Generic
Imports DevExpress.Persistent.Base
Imports DevExpress.ExpressApp.Model
Imports DevExpress.ExpressApp.Utils
Imports DevExpress.ExpressApp.Actions
Imports DevExpress.Persistent.BaseImpl
Imports DevExpress.ExpressApp.PivotChart
Imports DevExpress.ExpressApp.Templates

Namespace WinWebSolution.Module
	Public Class InplaceAnalysisCacheController
		Inherits WindowController
		Private analysisCache_Renamed As New LightDictionary(Of Type, List(Of Object))()
		Private Sub Window_ViewChanging(ByVal sender As Object, ByVal e As ViewChangingEventArgs)
			ClearCache()
		End Sub
		Public Sub New()
			TargetWindowType = WindowType.Main
		End Sub
		Protected Overrides Sub OnActivated()
			MyBase.OnActivated()
			AddHandler Window.ViewChanging, AddressOf Window_ViewChanging
		End Sub
		Protected ReadOnly Property AnalysisCache() As LightDictionary(Of Type, List(Of Object))
			Get
				Return analysisCache_Renamed
			End Get
		End Property
		Public Sub ClearCache()
			analysisCache_Renamed.Clear()
		End Sub
		Public Overridable Function GetAnalysisDataList(ByVal targetObjectType As Type) As List(Of Object)
            Dim cachedReports As List(Of Object) = Nothing
            If analysisCache_Renamed.TryGetValue(targetObjectType, cachedReports) Then
                Return cachedReports
            Else
                Using objectSpace As ObjectSpace = Application.CreateObjectSpace()
                    Dim targetObjectTypeNames As New List(Of String)()
                    Dim currentType As Type = targetObjectType
                    Do While (currentType IsNot GetType(Object)) AndAlso (currentType IsNot Nothing)
                        If XafTypesInfo.Instance.FindTypeInfo(currentType).IsPersistent Then
                            targetObjectTypeNames.Add(currentType.FullName)
                        End If
                        currentType = currentType.BaseType
                    Loop
                    Dim result As New List(Of Object)()
                    If targetObjectTypeNames.Count > 0 Then
                        Dim reports As IList = objectSpace.CreateCollection(GetType(Analysis), New InOperator("DataType", targetObjectTypeNames))
                        For Each report As Analysis In reports
                            result.Add(objectSpace.GetKeyValue(report))
                        Next report
                    End If
                    analysisCache_Renamed.Add(targetObjectType, result)
                    Return result
                End Using
            End If
		End Function
		Public Shared Function GetAnalysisDataList(ByVal xafApplication As XafApplication, ByVal targetObjectType As Type) As List(Of Object)
			Guard.ArgumentNotNull(xafApplication, "xafApplication")
			If xafApplication.MainWindow IsNot Nothing Then
				Dim cacheController As InplaceAnalysisCacheController = xafApplication.MainWindow.GetController(Of InplaceAnalysisCacheController)()
				If cacheController IsNot Nothing Then
					Return cacheController.GetAnalysisDataList(targetObjectType)
				End If
			End If
			Return New List(Of Object)()
		End Function
	End Class
	' Dennis: To use it you should first select the required records and then execute the ShowInAnalysisAction action.
	Public Class ShowInAnalysisViewController
		Inherits ViewController
		Private showInAnalysisActionCore As SingleChoiceAction
		Private Sub showInReportAction_Execute(ByVal sender As Object, ByVal e As SingleChoiceActionExecuteEventArgs)
			If View.SelectedObjects.Count = 0 Then
				Return
			End If
			ShowInAnalysis(e)
		End Sub
		Protected Sub ShowInAnalysis(ByVal e As SingleChoiceActionExecuteEventArgs)
			Dim os As ObjectSpace = Application.CreateObjectSpace()
			Dim report As Analysis = os.GetObjectByKey(Of Analysis)(e.SelectedChoiceActionItem.Data)
			e.ShowViewParameters.CreatedView = Application.CreateDetailView(os, report)
			e.ShowViewParameters.TargetWindow = TargetWindow.Default
			e.ShowViewParameters.Context = TemplateContext.View
			e.ShowViewParameters.CreateAllControllers = True
			Dim keys As New ArrayList()
			For Each selectedObject As Object In View.SelectedObjects
				keys.Add(ObjectSpace.GetKeyValue(selectedObject))
			Next selectedObject
			e.ShowViewParameters.Controllers.Add(New AssignCustomAnalysisDataSourceDetailViewController(New InOperator(ObjectSpace.GetKeyPropertyName(View.ObjectTypeInfo.Type), keys)))
		End Sub
		Private Function SortByCaption(ByVal left As ChoiceActionItem, ByVal right As ChoiceActionItem) As Integer
			Return Comparer(Of String).Default.Compare(left.Caption, right.Caption)
		End Function
		Protected Overrides Overloads Sub OnActivated()
			Dim os As ObjectSpace = Application.CreateObjectSpace()
			Dim reportList As List(Of Object) = InplaceAnalysisCacheController.GetAnalysisDataList(Application, View.ObjectTypeInfo.Type)
			Dim items As New List(Of ChoiceActionItem)()
			For Each id As Object In reportList
				Dim report As Analysis = os.GetObjectByKey(Of Analysis)(id)
				If report IsNot Nothing Then
					items.Add(New ChoiceActionItem(report.Name, id))
				End If
			Next id
			items.Sort(AddressOf SortByCaption)
			showInAnalysisActionCore.Items.Clear()
			showInAnalysisActionCore.Items.AddRange(items)
			UpdateActionActivity(showInAnalysisActionCore)
			MyBase.OnActivated()
		End Sub
		Protected Overrides Overloads Sub UpdateActionActivity(ByVal action As ActionBase)
			MyBase.UpdateActionActivity(action)
			Dim visibleInReportsModel As IModelClassReportsVisibility = TryCast(View.Model.ModelClass, IModelClassReportsVisibility)
			If visibleInReportsModel Is Nothing Then
				action.Active("VisibleInReports") = False
			Else
				action.Active("VisibleInReports") = visibleInReportsModel.IsVisibleInReports
			End If
		End Sub
		Public Sub New()
			showInAnalysisActionCore = New SingleChoiceAction(Me, "ShowInAnalysis", PredefinedCategory.RecordEdit)
			showInAnalysisActionCore.Caption = "Show in Analysis"
			showInAnalysisActionCore.ImageName = "Attention"
			showInAnalysisActionCore.PaintStyle = ActionItemPaintStyle.CaptionAndImage
			showInAnalysisActionCore.ToolTip = "Show selected records in a analysis"
			AddHandler showInAnalysisActionCore.Execute, AddressOf showInReportAction_Execute
			showInAnalysisActionCore.ItemType = SingleChoiceActionItemType.ItemIsOperation
			showInAnalysisActionCore.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects
		End Sub
		Protected Overrides Overloads Sub Dispose(ByVal disposing As Boolean)
			If disposing AndAlso showInAnalysisActionCore IsNot Nothing Then
				RemoveHandler showInAnalysisActionCore.Execute, AddressOf showInReportAction_Execute
			End If
			MyBase.Dispose(disposing)
		End Sub
		Public ReadOnly Property ShowInAnalysisAction() As SingleChoiceAction
			Get
				Return showInAnalysisActionCore
			End Get
		End Property
	End Class
	Public Class AssignCustomAnalysisDataSourceDetailViewController
		Inherits AnalysisViewControllerBase
		Private selectionCriteria As CriteriaOperator
		Public Sub New(ByVal criteria As CriteriaOperator)
			selectionCriteria = criteria
		End Sub
		Public Sub New()
		End Sub
		Protected Overrides Sub OnActivated()
			MyBase.OnActivated()
			' This event is fired after assigning a data source to the editor's control. So, you can handle it to filter only selected objects.
			AddHandler analysisEditor.DataSourceCreated, AddressOf analysisEditor_DataSourceCreated
		End Sub
		Private Sub analysisEditor_DataSourceCreated(ByVal sender As Object, ByVal e As DataSourceCreatedEventArgs)
			Dim dataSource As XPCollection = TryCast(e.DataSource, XPCollection)
			If dataSource IsNot Nothing Then
				dataSource.Criteria = dataSource.Criteria And selectionCriteria
			End If
		End Sub
		Protected Overrides Sub OnDeactivating()
			RemoveHandler analysisEditor.DataSourceCreated, AddressOf analysisEditor_DataSourceCreated
			MyBase.OnDeactivating()
		End Sub
	End Class
End Namespace