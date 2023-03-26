using AntDesign;
using BcdLib.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BasicKube;

public abstract class CreateResForm: BcdForm
{
    protected MessageService MessageService = default!;
    protected INotificationService NotificationService = default!;

    public int IamId { get; set; }
    public bool IsEdit { get; set; } = false;

    protected string MsgPrefix => IsEdit ? "编辑" : "创建";



    public Func<Task>? OnOk { get; set; }

    public Func<Task>? OnCancel { get; set; }


    protected override void InitComponent()
    {
        Width = 1000;
        MinimizeBox = false;
        BodyStyle = "height: calc(100vh - 150px)";
        //BodyStyle = "max-height: 80vh;";
        Centered = true;
        Footer = CreateFooter;
        MessageService = ServiceProvider.GetRequiredService<MessageService>();
        NotificationService = ServiceProvider.GetRequiredService<INotificationService>();
        ShowMask = true;
        MaskClosable = false;
        //StickyFooter = true;
    }

    private void CreateFooter(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CreateResFormFooter>(0);
        builder.AddAttribute(1, "OnOk",
                EventCallback.Factory.Create<MouseEventArgs>(this, async (e) =>
                {
                    await OnOkAsync();
                })
            );
        builder.AddAttribute(2, "OnCancel",
                EventCallback.Factory.Create<MouseEventArgs>(this, async (e) =>
                {
                    await OnCancelAsync();
                })
            );
        builder.CloseComponent();
    }

    protected async Task NoticeSuccess(string content, string title = "Info")
    {
        await NotificationService.Open(new NotificationConfig()
        {
            Message = title,
            Description = content,
            NotificationType = NotificationType.Success
        });
    }

    protected async Task NoticeError(string content, string title = "Error")
    {
        await NotificationService.Open(new NotificationConfig()
        {
            Message = title,
            Description = content,
            NotificationType = NotificationType.Error
        });
    }

    protected abstract Task OnOkAsync();

    protected virtual async Task OnCancelAsync()
    {
        if (OnCancel != null)
        {
            await OnCancel();
        }

        await this.CloseAsync();
    }
}
