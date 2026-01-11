using System;

namespace PortPilot_Project.Abstractions;

public interface ITrayController : IDisposable
{
    void Initialize();
    void ShowWindow();
    void HideWindow();
    void ExitApplication();
}
