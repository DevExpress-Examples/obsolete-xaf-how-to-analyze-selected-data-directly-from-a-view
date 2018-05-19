Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports DevExpress.Data.Filtering
Imports DevExpress.ExpressApp.Actions
Imports DevExpress.Persistent.Base
Imports System.Collections.Generic
Imports DevExpress.ExpressApp.Utils
Imports DevExpress.Persistent.BaseImpl
Imports DevExpress.ExpressApp.PivotChart
Imports DevExpress.ExpressApp

Namespace WinWebSolution.Module
	Public Class InplaceAnalysisCacheController
		Inherits WindowController
		Private analysisCache_Renamed As LightDictionary(Of Type, List(Of Object)) = New LightDictionary(Of Type, List(Of Object))()
		Private Sub Window_ViewChanging(ByVal sender As Object, ByVal e As EventArgs)
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
			Dim cachedReports As List(Of Object)
			If analysisCache_Renamed.TryGetValue(targetObjectType, cachedReports) Then
				Return cachedReports
			Else
				Using objectSpace As ObjectSpace = Application.CreateObjectSpace()
					Dim targetObjectTypeNames As List(Of String) = New List(Of String)()
					Dim currentType As Type = targetObjectType
					Do While (currentType IsNot GetType(Object)) AndAlso (currentType IsNot Nothing)
						If XafTypesInfo.Instance.FindTypeInfo(currentType).IsPersistent Then
							targetObjectTypeNames.Add(currentType.FullName)
						End If
						currentType = currentType.BaseType
					Loop
					Dim result As List(Of Object) = New List(Of Object)()
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
		Protected Overrides Sub OnActivated()
			Dim os As ObjectSpace = Application.CreateObjectSpace()
			Dim reportList As List(Of Object) = InplaceAnalysisCacheController.GetAnalysisDataList(Application, View.ObjectTypeInfo.Type)
			Dim items As List(Of ChoiceActionItem) = New List(Of ChoiceActionItem)()
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
		Protected Overrides Sub UpdateActionActivity(ByVal action As ActionBase)
			MyBase.UpdateActionActivity(action)
			action.Active("VisibleInReports") = Application.FindClassInfo(View.ObjectTypeInfo.Type).GetAttributeBoolValue("VisibleInReports")
		End Sub
		Public Sub New()
			showInAnalysisActionCore = New SingleChoiceAction(Me, "ShowInAnalysis", PredefinedCategory.RecordEdit)
			showInAnalysisActionCore.Caption = "Show in Analysis"
			showInAnalysisActionCore.ToolTip = "Show selected records in a analysis"
			AddHandler showInAnalysisActionCore.Execute, AddressOf showInReportAction_Execute
			showInAnalysisActionCore.ItemType = SingleChoiceActionItemType.ItemIsOperation
			showInAnalysisActionCore.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects
			showInAnalysisActionCore.ItemHierarchyType = ChoiceActionItemHierarchyType.Tree
		End Sub
		Protected Overrides Overloads Sub Dispose(ByVal disposing As Boolean)
			If disposing Then
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
		Private selectionCriteria As CriteriaOperator = Nothing
		Public Sub New()
			MyBase.New()
		End Sub
		Public Sub New(ByVal criteria As CriteriaOperator)
			Me.selectionCriteria = criteria
		End Sub
		Protected Overrides Sub OnActivated()
			MyBase.OnActivated()
			' This event is fired when assigning a data source to the editor's control. So, you can handle it to provide your own data source.
			AddHandler analysisEditor.DataSourceCreating, AddressOf analysisEditor_DataSourceCreating
		End Sub
		Private Sub analysisEditor_DataSourceCreating(ByVal sender As Object, ByVal e As DataSourceCreatingEventArgs)
			Dim userCriteria As CriteriaOperator = Nothing
			If (Not String.IsNullOrEmpty(e.AnalysisInfo.Criteria)) Then
				' This is a wrapper class that parses the XAF's "native" criteria strings and converts "these XAF's things" by replacing them with actual values into a normal XPO criteria string.
				userCriteria = CriteriaWrapper.ParseCriteriaWithReadOnlyParameters(e.AnalysisInfo.Criteria, e.AnalysisInfo.DataType)
			End If
			' After that you can reach the normal (understandable to XPO) criteria and use it to filter a collection.
			e.DataSource = View.ObjectSpace.CreateCollection(e.AnalysisInfo.DataType, userCriteria And selectionCriteria)
			' We need to set this parameter to true to notify that we completely handled the event and provided the data source to the editor's control.
			e.Handled = True
		End Sub
	End Class
End Namespace