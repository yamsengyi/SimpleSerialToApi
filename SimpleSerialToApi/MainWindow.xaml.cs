using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SimpleSerialToApi.ViewModels;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // MainWindow Closing 이벤트 핸들러 추가
                this.Closing += MainWindow_Closing;
                
                // Simple DataContext setup
                var app = (App)System.Windows.Application.Current;
                if (app?.ServiceProvider != null)
                {
                    DataContext = app.ServiceProvider.GetRequiredService<MainViewModel>();
                }
                
                // 메뉴 상태 업데이트
                UpdateStartupMenus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"MainWindow initialization failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // MainViewModel에 모든 자식 창 닫기 요청
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.CloseAllChildWindows();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing child windows: {ex.Message}");
            }
        }

        private void UpdateStartupMenus()
        {
            try
            {
                var app = (App)System.Windows.Application.Current;
                if (app?.ServiceProvider != null)
                {
                    var startupService = app.ServiceProvider.GetRequiredService<StartupService>();
                    var isEnabled = startupService.IsStartupEnabled();
                    
                    // 메뉴 아이템을 이름으로 찾기
                    var registerMenu = FindName("MenuStartupRegister") as MenuItem;
                    var unregisterMenu = FindName("MenuStartupUnregister") as MenuItem;
                    
                    if (registerMenu != null)
                        registerMenu.IsEnabled = !isEnabled;
                    if (unregisterMenu != null)
                        unregisterMenu.IsEnabled = isEnabled;
                }
            }
            catch (Exception ex)
            {
                // 로그 처리만 하고 UI는 기본 상태로 유지
                System.Diagnostics.Debug.WriteLine($"메뉴 상태 업데이트 오류: {ex.Message}");
            }
        }

        private void StartupRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = (App)System.Windows.Application.Current;
                if (app?.ServiceProvider != null)
                {
                    var startupService = app.ServiceProvider.GetRequiredService<StartupService>();
                    
                    var result = System.Windows.MessageBox.Show(
                        "윈도우 시작시 자동 실행을 등록하시겠습니까?\n\n프로그램은 최소화 상태로 트레이에서 시작됩니다.", 
                        "시작 프로그램 등록", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = startupService.EnableStartup(true);
                        System.Windows.MessageBox.Show(
                            success ? "시작 프로그램으로 등록되었습니다.\n(최소화 상태로 시작)" : "시작 프로그램 등록에 실패했습니다.", 
                            "등록 결과", 
                            MessageBoxButton.OK, 
                            success ? MessageBoxImage.Information : MessageBoxImage.Error);
                        
                        if (success)
                            UpdateStartupMenus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"시작 프로그램 등록 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartupUnregister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = (App)System.Windows.Application.Current;
                if (app?.ServiceProvider != null)
                {
                    var startupService = app.ServiceProvider.GetRequiredService<StartupService>();
                    
                    var result = System.Windows.MessageBox.Show(
                        "윈도우 시작 프로그램에서 제거하시겠습니까?", 
                        "시작 프로그램 삭제", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = startupService.DisableStartup();
                        System.Windows.MessageBox.Show(
                            success ? "시작 프로그램에서 제거되었습니다." : "시작 프로그램 제거에 실패했습니다.", 
                            "삭제 결과", 
                            MessageBoxButton.OK, 
                            success ? MessageBoxImage.Information : MessageBoxImage.Error);
                        
                        if (success)
                            UpdateStartupMenus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"시작 프로그램 삭제 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckStartupStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = (App)System.Windows.Application.Current;
                if (app?.ServiceProvider != null)
                {
                    var startupService = app.ServiceProvider.GetRequiredService<StartupService>();
                    var isEnabled = startupService.IsStartupEnabled();
                    var command = startupService.GetStartupCommand();
                    
                    var message = isEnabled 
                        ? $"✅ 윈도우 시작 프로그램으로 등록되어 있습니다.\n\n등록된 명령어:\n{command}"
                        : "❌ 윈도우 시작 프로그램으로 등록되어 있지 않습니다.";
                    
                    System.Windows.MessageBox.Show(message, "시작 프로그램 상태", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"상태 확인 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenRegistry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "레지스트리 편집기를 열어 시작 프로그램 목록을 확인하시겠습니까?\n\n경로: HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", 
                    "레지스트리 열기", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("regedit.exe", "/s");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"레지스트리 편집기 실행 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TraySettings_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("트레이 설정 기능은 개발 중입니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Simple Serial To API v1.0\n\n시리얼 데이터를 API로 전송하는 애플리케이션", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}