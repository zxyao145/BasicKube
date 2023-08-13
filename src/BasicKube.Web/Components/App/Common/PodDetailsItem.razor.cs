using AntDesign.TableModels;
using BasicKube.Api.Common;
using BasicKube.Web.Services;
using Microsoft.AspNetCore.Components;

namespace BasicKube.Web.Components;

public partial class PodDetailsItem
{
    [Inject] 
    public KubeHttpClient KubeHttpClient { get; set; } = default!;

    [CascadingParameter(Name = "IamId")]
    public int IamId { get; set; }

    public string EnvName => AppName.Split("-")[^1];

    [Parameter]
    public List<PodDetail> PodDetails { get; set; } = new();

    [Parameter]
    public string AppName { get; set; } = "";

    [Parameter]
    public bool ShowDelBtn { get; set; } = true;

    #region table events

    private async Task OnDelPodAsync(PodDetail podInfo)
    {
        await KubeHttpClient.PodHttp.Del(IamId, podInfo.Name);
    }

    private async Task OnRowExpand(RowData<PodDetail> rowData)
    {
        if (rowData.Data.ContainerDetails != null)
        {
            return;
        }

        await Task.Delay(1);
        StateHasChanged();
    }

    #endregion

    #region pod events

    // pod events
    private List<EventInfo> _events = new List<EventInfo>();

    bool _visible = false;

    async Task ShowEventsAsync(PodDetail podDetail)
    {
        this._visible = true;
        _events = await KubeHttpClient.EventsHttp.GetEvents(IamId, podDetail.Name);
        await InvokeAsync(StateHasChanged);
    }

    void Close()
    {
        this._visible = false;
    }

    #endregion


    private Timer _timer;

    protected override void OnInitialized()
    {
        TimerCallback timerCallback = QueryPodBasicMetric;
        _timer = new Timer
        (
            timerCallback,
            null, 
            TimeSpan.FromSeconds(0), 
            TimeSpan.FromSeconds(30)
        );
    }

    private string? _grpName;
    private Dictionary<string, string[]> _mainContainerMetrics = new();
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (_mainContainerMetrics.Count != PodDetails.Count)
        {
            foreach (PodDetail podDetail in PodDetails)
            {
                _mainContainerMetrics.TryAdd(podDetail.Name, new string[2]);
            }
        }
    }

    private async void QueryPodBasicMetric(object? state)
    {
        if (string.IsNullOrWhiteSpace(_grpName))
        {
            _grpName = K8sUtil.GetGrpNameByAppName(AppName);
        }
        var data = await KubeHttpClient.MetricHttp.ListWithEnv(IamId, EnvName, _grpName);

        foreach (var item in data)
        {
            var val = item.Value;
            _mainContainerMetrics.TryAdd(item.Key, new string[2]);
            _mainContainerMetrics[item.Key][0]
                = val.ContainerMetrics["main"].Cpu;
            _mainContainerMetrics[item.Key][1]
                = val.ContainerMetrics["main"].Memory;

            //Console.WriteLine(val.ContainerMetrics["main"].Memory);
            StateHasChanged();
        }
    }
}
