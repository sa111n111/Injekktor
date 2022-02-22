using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Injekktor
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        public Form1()
        {
            InitializeComponent();
        }

        private bool AddSecurityControl(string dllPath)
        {
            if(checkBox1.Checked)
            {
                FileInfo fileInfo = new FileInfo(dllPath);
                System.Security.AccessControl.FileSecurity fileSec = fileInfo.GetAccessControl();
                fileSec.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule("ALL APPLICATION PACKAGES", System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
                fileInfo.SetAccessControl(fileSec);
            } 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;

            label1.Text = "Injekktor";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.Font = new Font(label1.Font, FontStyle.Bold);

            label2.Text = "Enter DLL path";

            label3.Text = "Enter Process Name";

            button1.Text = "Inject!";
            checkBox1.Text = "UWP Mode";
        }

        // Custom draggable asset
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            panel1.BackColor = Color.Gray;
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Inject();
        }

        private async void Inject()
        {
            textBox1.Multiline = true;
            bool pathProvided = textBox1.Text.Length != 0;
            bool processProvided = textBox2.Text.Length != 0;

            if (!pathProvided || !processProvided)
            {
                MessageBox.Show("No path or processname provided.");
                Application.Exit();
            }
            
            string procName = textBox2.Text;
            string dllPath = textBox1.Text;

            AddSecurityControl(dllPath);

            try
            {
                Process process = Process.GetProcessesByName(procName)[0];
                IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, process.Id);

                IntPtr loadLibraryA = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                IntPtr allocatedMemory = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                UIntPtr bytesWritten;
                WriteProcessMemory(procHandle, allocatedMemory, Encoding.Default.GetBytes(dllPath), (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);
                CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryA, allocatedMemory, 0, IntPtr.Zero);
                label1.Text = "Injected!";

            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
