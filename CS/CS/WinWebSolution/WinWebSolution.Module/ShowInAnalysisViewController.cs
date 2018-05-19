using System;
using System.Collections;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using System.Collections.Generic;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.BaseImpl;
using DevExpress.ExpressApp.PivotChart;
using DevExpress.ExpressApp;

namespace WinWebSolution.Module {
    public class InplaceAnalysisCacheController : WindowController {
        private LightDictionary<Type, List<object>> analysisCache = new LightDictionary<Type, List<object>>();
        void Window_ViewChanging(object sender, EventArgs e) {
            ClearCache();
        }
        public InplaceAnalysisCacheController() {
            TargetWindowType = WindowType.Main;
        }
        protected override void OnActivated() {
            base.OnActivated();
            Window.ViewChanging += new EventHandler(Window_ViewChanging);
        }
        protected LightDictionary<Type, List<object>> AnalysisCache {
            get { return analysisCache; }
        }
        public void ClearCache() {
            analysisCache.Clear();
        }
        public virtual List<object> GetAnalysisDataList(Type targetObjectType) {
            List<object> cachedReports;
            if (analysisCache.TryGetValue(targetObjectType, out cachedReports)) {
                return cachedReports;
            } else {
                using (ObjectSpace objectSpace = Application.CreateObjectSpace()) {
                    List<string> targetObjectTypeNames = new List<string>();
                    Type currentType = targetObjectType;
                    while ((currentType != typeof(object)) && (currentType != null)) {
                        if (XafTypesInfo.Instance.FindTypeInfo(currentType).IsPersistent) {
                            targetObjectTypeNames.Add(currentType.FullName);
                        }
                        currentType = currentType.BaseType;
                    }
                    List<object> result = new List<object>();
                    if (targetObjectTypeNames.Count > 0) {
                        IList reports = objectSpace.CreateCollection(typeof(Analysis), new InOperator("DataType", targetObjectTypeNames));
                        foreach (Analysis report in reports) {
                            result.Add(objectSpace.GetKeyValue(report));
                        }
                    }
                    analysisCache.Add(targetObjectType, result);
                    return result;
                }
            }
        }
        public static List<object> GetAnalysisDataList(XafApplication xafApplication, Type targetObjectType) {
            Guard.ArgumentNotNull(xafApplication, "xafApplication");
            if (xafApplication.MainWindow != null) {
                InplaceAnalysisCacheController cacheController = xafApplication.MainWindow.GetController<InplaceAnalysisCacheController>();
                if (cacheController != null) {
                    return cacheController.GetAnalysisDataList(targetObjectType);
                }
            }
            return new List<object>();
        }
    }
    // Dennis: To use it you should first select the required records and then execute the ShowInAnalysisAction action.
    public class ShowInAnalysisViewController : ViewController {
        private SingleChoiceAction showInAnalysisActionCore;
        private void showInReportAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e) {
            if (View.SelectedObjects.Count == 0) {
                return;
            }
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
                if (report != null) {
                    items.Add(new ChoiceActionItem(report.Name, id));
                }
            }
            items.Sort(SortByCaption);
            showInAnalysisActionCore.Items.Clear();
            showInAnalysisActionCore.Items.AddRange(items);
            UpdateActionActivity(showInAnalysisActionCore);
            base.OnActivated();
        }
        protected override void UpdateActionActivity(ActionBase action) {
            base.UpdateActionActivity(action);
            action.Active["VisibleInReports"] = Application.FindClassInfo(View.ObjectTypeInfo.Type).GetAttributeBoolValue("VisibleInReports");
        }
        public ShowInAnalysisViewController() {
            showInAnalysisActionCore = new SingleChoiceAction(this, "ShowInAnalysis", PredefinedCategory.RecordEdit);
            showInAnalysisActionCore.Caption = "Show in Analysis";
            showInAnalysisActionCore.ToolTip = "Show selected records in a analysis";
            showInAnalysisActionCore.Execute += new SingleChoiceActionExecuteEventHandler(showInReportAction_Execute);
            showInAnalysisActionCore.ItemType = SingleChoiceActionItemType.ItemIsOperation;
            showInAnalysisActionCore.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            showInAnalysisActionCore.ItemHierarchyType = ChoiceActionItemHierarchyType.Tree;
        }
        protected override void Dispose(bool disposing) {
            if (disposing) {
                showInAnalysisActionCore.Execute -= new SingleChoiceActionExecuteEventHandler(showInReportAction_Execute);
            }
            base.Dispose(disposing);
        }
        public SingleChoiceAction ShowInAnalysisAction {
            get { return showInAnalysisActionCore; }
        }
    }
    public class AssignCustomAnalysisDataSourceDetailViewController : AnalysisViewControllerBase {
        private CriteriaOperator selectionCriteria = null;
        public AssignCustomAnalysisDataSourceDetailViewController() : base() { }
        public AssignCustomAnalysisDataSourceDetailViewController(CriteriaOperator criteria) {
            this.selectionCriteria = criteria;
        }
        protected override void OnActivated() {
            base.OnActivated();
            // This event is fired when assigning a data source to the editor's control. So, you can handle it to provide your own data source.
            this.analysisEditor.DataSourceCreating += new EventHandler<DataSourceCreatingEventArgs>(analysisEditor_DataSourceCreating);
        }
        void analysisEditor_DataSourceCreating(object sender, DataSourceCreatingEventArgs e) {
            CriteriaOperator userCriteria = null;
            if (!string.IsNullOrEmpty(e.AnalysisInfo.Criteria)) {
                // This is a wrapper class that parses the XAF's "native" criteria strings and converts "these XAF's things" by replacing them with actual values into a normal XPO criteria string.
                userCriteria = CriteriaWrapper.ParseCriteriaWithReadOnlyParameters(e.AnalysisInfo.Criteria, e.AnalysisInfo.DataType);
            }
            // After that you can reach the normal (understandable to XPO) criteria and use it to filter a collection.
            e.DataSource = View.ObjectSpace.CreateCollection(e.AnalysisInfo.DataType, userCriteria & selectionCriteria);
            // We need to set this parameter to true to notify that we completely handled the event and provided the data source to the editor's control.
            e.Handled = true;
        }
    }
}