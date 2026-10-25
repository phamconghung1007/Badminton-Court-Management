using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BadmintonManager.ViewModels.Base;

public partial class PlaceholderPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = "CalendarClock";

    [ObservableProperty]
    private string _dayTarget = "Theo lộ trình";

    public PlaceholderPageViewModel(string title, string description, string dayTarget = "Theo lộ trình", string icon = "CalendarClock")
    {
        Title = title;
        Description = description;
        DayTarget = dayTarget;
        Icon = icon;
    }
}
