namespace FluentRoslyn.CSharp.Tests;

public static class TestHelpers
{
    public static IReadOnlyCollection<string> NormaliseSource(string fileContents)
    {
        return fileContents
            .Split(Environment.NewLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray();
    }

  public static string code2 = @"
public class Test{
public void BindText() {
textEdit1.BindText(ViewModel.Text1);
}

public void BinButton() {
button1.BinButton(ViewModel.SomeTask());
}

public void BindGridView() {
gridControl1
.BindData(ViewModel.SourceData)
.BindSelected(ViewModel.SourceDataSelected)
;
}

public void BindComboBox() {
cboArea.BindComboBox();
}

public void InitSomeBind() {

}
}
";

  public static string PMS = """
        [ViewType("vtPMS")]
    public partial class frmPMSMain : CommonForm
    {
    	public frmPMSMain()
    	{
    		InitializeComponent();
    		InitializeBindings();
    		this.SetFormStyle()
    			.AdjustWindowSizeAndPosition();
    		btnCancel.ButtonBlack();
    	}

    	private void InitializeBindings()
    	{
    		var fluent = this.SetMvvm<frmPMSVM>();
    		fluent.SetBinding(this, c => c.IsLoading, x => x.IsLoading);

    		fluent.SetBinding(cboArea.Properties, p => p.DataSource, x => x.ComboxEquipment.LstShop);
    		fluent.SetBinding(cboEqpType.Properties, p => p.DataSource, x => x.ComboxEquipment.LstEqpType);
    		fluent.SetBinding(cboEqp.Properties, p => p.DataSource, x => x.ComboxEquipment.LstEqpName);
    		fluent.SetBinding(cboScheduleType.Properties, p => p.DataSource, x => x.ComboxEquipment.LstScheduleType);
    		fluent.SetBinding(cboArea, c => c.Text, x => x.ComboxEquipment.Shop);
    		fluent.SetBinding(cboEqpType, c => c.Text, x => x.ComboxEquipment.EqpType);
    		fluent.SetBinding(cboEqp, c => c.Text, x => x.EqpName);

    		fluent.SetBinding(btnDelete, c => c.Enabled, x => x.HasValue);

    		fluent.SetBinding(cboScheduleType, c => c.Text, x => x.ComboxEquipment.ScheduleType);

    		fluent.SetPagedGridViewControl(gridControl1)
    			.SetRowClick(x => x.DtScheduleInfo_Selected)
    			.SetPageChangedFunc(x => x.ScheduleTypeChanged)
    			.SetTotalCount(x => x.ScheduleTotalCount)
    			.SetMaxResultCount(x => x.MaxResultCount)
    			.SetSkipCount(x => x.SkipCount)
    			.SetDataSource(x => x.DtScheduleInfo);

    		fluent.SetPagedGridViewControl(gridControl2)
    			.SetRowClick(x => x.DtWorkOrderInfo_Selected)
    			.SetPageChangedFunc(x => x.WorkOrderTypeChanged)
    			.SetTotalCount(x => x.WorkOrderTotalCount)
    			.SetMaxResultCount(x => x.WorkOrderMaxResultCount)
    			.SetSkipCount(x => x.WorkOrderSkipCount)
    			.SetDataSource(x => x.DtWorkOrderInfo);

    		fluent.SetStartTimeStyle(dtFrom, x => x.ComboxEquipment.FromDate);
    		fluent.SetEndTimeStyle(dtEnd, x => x.ComboxEquipment.EndDate);

    		fluent.WithEvent(btnQuery, nameof(btnQuery.Click)).EventToCommand(x => x.BtnQuery);
    		fluent.WithEvent(bWorkTemplate, nameof(bWorkTemplate.Click)).EventToCommand(x => x.BtnWorkTemplateTrigCode);
    		fluent.WithEvent(btnAddTimeSchedule, nameof(btnQuery.Click)).EventToCommand(x => x.BtnAddTimeSchedule);
    		fluent.WithEvent(btnAddTrigSchedule, nameof(btnQuery.Click)).EventToCommand(x => x.BtnAddTrigCodeSchedule);
    		fluent.WithEvent(btnDelete, nameof(btnQuery.Click)).EventToCommand(x => x.BtnDelete);
    		fluent.WithEvent(btnUpdate, nameof(btnQuery.Click)).EventToCommand(x => x.BtnModify);
    		fluent.WithEvent(bHistory, nameof(btnQuery.Click)).EventToCommand(x => x.BtnHistory);
    		fluent.WithEvent(bTriggerCode, nameof(btnQuery.Click)).EventToCommand(x => x.BtnQueryTrigCode);
    		fluent.WithEvent(btnAddWorkOrder, nameof(btnQuery.Click)).EventToCommand(x => x.BtnAddWorkOrder);

    		fluent.WithEvent(btnClockOn, nameof(btnQuery.Click)).EventToCommand(x => x.BtnClockOn);
    		fluent.WithEvent(btnClockOff, nameof(btnQuery.Click)).EventToCommand(x => x.BtnClockOff);
    		fluent.WithEvent(btnComplete, nameof(btnQuery.Click)).EventToCommand(x => x.BtnComplete);
    		fluent.WithEvent(btnCancel, nameof(btnQuery.Click)).EventToCommand(x => x.BtnCancel);
    		fluent.WithEvent(btnWorkOrderHistory, nameof(btnQuery.Click)).EventToCommand(x => x.BtnWorkOrderHistory);
    		fluent.WithEvent(btnDetail, nameof(btnQuery.Click)).EventToCommand(x => x.BtnPMWork);

    		fluent.WithEvent(this, (nameof(this.Load))).EventToCommand(x => x.LoadAsync);
    	}
    }
    """;
}
