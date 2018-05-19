using System;
using DevExpress.Xpo;
using System.Collections;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using System.Collections.Generic;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.BaseImpl;
using DevExpress.ExpressApp.PivotChart;
using DevExpress.ExpressApp.Templates;

namespace WinWebSolution.Module {
    public class InplaceAnalysisCacheController : WindowController {
        private LightDictionary<Type, List<object>> analysisCache = new LightDictionary<Type, List<object>>();
        void Window_ViewChanging(object sender, ViewChangingEventArgs e) {
            ClearCache();
        }
        public InplaceAnalysisCacheController() {
            TargetWindowType = WindowType.Main;
        }
        protected override void OnActivated() {
            base.OnActivated();
            Window.ViewChanging += Window_ViewChanging;
        }
        protected LightDictionary<Type, List<object>> AnalysisCache {
            get { return analysisCache; }
        }
        public void ClearCache() {
            analysisCache.Clear();
        }
        public virtual List<object> GetAnalysisDataList(Type targetObjectType) {
            List<object> cachedReports;
            if (analysisCache.TryGetValue(targetObjectType, out cachedReports))
                return cachedReports;
            else
                using (ObjectSpace objectSpace = Application.CreateObjectSpace()) {
                    List<string> targetObjectTypeNames = new List<string>();
                    Type currentType = targetObjectType;
                    while ((currentType != typeof(object)) && (currentType != null)) {
                        if (XafTypesInfo.Instance.FindTypeInfo(currentType).IsPersistent)
                            targetObjectTypeNames.Add(currentType.FullName);
                        currentType = currentType.BaseType;
                    }
                    List<object> result = new List<object>();
                    if (targetObjectTypeNames.Count > 0) {
                        IList reports = objectSpace.CreateCollection(typeof(Analysis), new InOperator("DataType", targetObjectTypeNames));
                        foreach (Analysis report in reports)
                            result.Add(objectSpace.GetKeyValue(report));
                    }
                    analysisCache.Add(targetObjectType, result);
                    return result;
                }
        }
        public static List<object> GetAnalysisDataList(XafApplication xafApplication, Type targetObjectType) {
            Guard.ArgumentNotNull(xafApplication, "xafApplication");
            if (xafApplication.MainWindow != null) {
                InplaceAnalysisCacheController cacheController = xafApplication.MainWindow.GetController<InplaceAnalysisCacheController>();
                if (cacheController != null)
                    return cacheController.GetAnalysisDataList(targetObjectType);
            }
            return new List<object>();
        }
    }
    // Dennis: To use it you should first select the required records and then execute the ShowInAnalysisAction action.
    public class ShowInAnalysisViewController : ViewController {
        private SingleChoiceAction showInAnalysisActionCore;
        private void showInReportAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e) {
            if (View.SelectedObjects.Count == 0) return;
            ShowInAnalysis(e);
        }
        protected void ShowInAnalysis(SingleChoiceActionExecuteEventArgs e) {
            ObjectSpace os = Application.CreateObjectSpace();
            Analysis report = os.GetObjectByKey<Analysis>(e.SelectedChoiceActionItem.Data);
            e.ShowViewParameters.CreatedView = Application.CreateDetailView(os, report);
            e.ShowViewParameters.TargetWindow = TargetWindow.Default;
            e.ShowViewParameters.Context = TemplateContext.View;
            e.ShowViewParameters.CreateAllControllers = true;
            ArrayList keys = new ArrayList();
            foreach (object selectedObject in View.SelectedObjects) {
                keys.Add(ObjectSpace.GetKeyValue(selectedObject));
            }
            e.ShowViewParameters.Controllers.Add(new AssignCustomAnalysisDataSourceDetailViewController(new InOperator(ObjectSpace.GetKeyPropertyName(View.ObjectTypeInfo.Type), keys)));
        }
        private int SortByCaption(ChoiceActionItem left, ChoiceActionItem right) {
            return Comparer<string>.Default.Compare(left.Caption, right.Caption);
        }
        protected override void OnActivated() {
            ObjectSpace os = Application.CreateObjectSpace();
            List<object> reportList = InplaceAnalysisCacheController.GetAnalysisDataList(Application, View.ObjectTypeInfo.Type);
            List<ChoiceActionItem> items = new List<ChoiceActionItem>();
            foreach (object id in reportList) {
                Analysis report = os.GetObjectByKey<Analysis>(id);
                if (report != null)
                    items.Add(new ChoiceActionItem(report.Name, id));
            }
            items.Sort(SortByCaption);
            showInAnalysisActionCore.Items.Clear();
            showInAnalysisActionCore.Items.AddRange(items);
            UpdateActionActivity(showInAnalysisActionCore);
            base.OnActivated();
        }
        protected override void UpdateActionActivity(ActionBase action) {
            base.UpdateActionActivity(action);
            IModelClassReportsVisibility visibleInReportsModel = View.Model.ModelClass as IModelClassReportsVisibility;
            action.Active["VisibleInReports"] = visibleInReportsModel == null ? false : visibleInReportsModel.IsVisibleInReports;
        }
        public ShowInAnalysisViewController() {
            showInAnalysisActionCore = new SingleChoiceAction(this, "ShowInAnalysis", PredefinedCategory.RecordEdit);
            showInAnalysisActionCore.Caption = "Show in Analysis";
            showInAnalysisActionCore.ImageName = "Attention";
            showInAnalysisActionCore.PaintStyle = ActionItemPaintStyle.CaptionAndImage;
            showInAnalysisActionCore.ToolTip = "Show selected records in a analysis";
            showInAnalysisActionCore.Execute += showInReportAction_Execute;
            showInAnalysisActionCore.ItemType = SingleChoiceActionItemType.ItemIsOperation;
            showInAnalysisActionCore.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
        }
        protected override void Dispose(bool disposing) {
            if (disposing && showInAnalysisActionCore != null)
                showInAnalysisActionCore.Execute -= showInReportAction_Execute;
            base.Dispose(disposing);
        }
        public SingleChoiceAction ShowInAnalysisAction {
            get { return showInAnalysisActionCore; }
        }
    }
    public class AssignCustomAnalysisDataSourceDetailViewController : AnalysisViewControllerBase {
        private CriteriaOperator selectionCriteria;
        public AssignCustomAnalysisDataSourceDetailViewController(CriteriaOperator criteria) {
            selectionCriteria = criteria;
        }
        public AssignCustomAnalysisDataSourceDetailViewController() { }
        protected override void OnActivated() {
            base.OnActivated();
            // This event is fired after assigning a data source to the editor's control. So, you can handle it to filter only selected objects.
            analysisEditor.DataSourceCreated += analysisEditor_DataSourceCreated;
        }
        private void analysisEditor_DataSourceCreated(object sender, DataSourceCreatedEventArgs e) {
            XPCollection dataSource = e.DataSource as XPCollection;
            if (dataSource != null)
                dataSource.Criteria &= selectionCriteria;
        }
        protected override void OnDeactivating() {
            analysisEditor.DataSourceCreated -= analysisEditor_DataSourceCreated;
            base.OnDeactivating();
        }
    }
}