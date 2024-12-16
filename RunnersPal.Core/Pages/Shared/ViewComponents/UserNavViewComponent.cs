using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Pages.ViewModels;

namespace RunnersPal.Core.Pages.Shared.ViewComponents;

public class UserNavViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync()
        => Task.FromResult(View(new UserNavModel(Request)) as IViewComponentResult);
}