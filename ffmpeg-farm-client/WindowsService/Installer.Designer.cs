namespace FFmpegFarm.WindowsService
{
    partial class Installer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.eventLogInstaller1 = new System.Diagnostics.EventLogInstaller();
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "Host for worker node threads";
            this.serviceInstaller1.ServiceName = "FFmpegFarmWorker";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // eventLogInstaller1
            // 
            this.eventLogInstaller1.CategoryCount = 0;
            this.eventLogInstaller1.CategoryResourceFile = null;
            this.eventLogInstaller1.Log = "FFmpegFarm";
            this.eventLogInstaller1.MessageResourceFile = null;
            this.eventLogInstaller1.ParameterResourceFile = null;
            this.eventLogInstaller1.Source = "Client";
            this.eventLogInstaller1.UninstallAction = System.Configuration.Install.UninstallAction.NoAction;
            // 
            // Installer
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceInstaller1,
            this.serviceProcessInstaller1,
            this.eventLogInstaller1});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.Diagnostics.EventLogInstaller eventLogInstaller1;
    }
}