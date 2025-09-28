using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Revit2Gltf.utils
{
    public class ExecResult
    {
        public string Output { get; set; }
        /// <summary>
        /// ��������ִ�к�Ĵ����������Ҫ����ʵ�������ж��Ƿ�ɹ������OutputΪ�յ�Error��Ϊ�գ����������˵���������������󣬵��ǿ�������ִ�н���
        /// </summary>
        public string Error { get; set; }
        /// <summary>
        /// ִ�з������쳣����ʾ����û������ִ�в�����
        /// </summary>
        public Exception ExceptError { get; set; }
    }


    /// <summary>
    /// ִ��cmd����
    /// </summary>
    public static class ESCmd
    {
        /// <summary>
        /// ִ��cmd���� ����cmd������ʾ����Ϣ
        /// ��������ʹ���������������ӷ���
        /// <![CDATA[
        /// &:ͬʱִ����������
        /// |:����һ����������,��Ϊ��һ�����������
        /// &&����&&ǰ������ɹ�ʱ,��ִ��&&�������
        /// ||����||ǰ������ʧ��ʱ,��ִ��||�������]]>
        /// </summary>
        ///<param name="command">ִ�е�����</param>
        ///<param name="workDirectory">����Ŀ¼</param>
        public static ExecResult Run(string command, string workDirectory = null)
        {
            //˵�������������Ƿ�ɹ���ִ��exit������򵱵���ReadToEnd()����ʱ���ᴦ�ڼ���״̬
            command = command.Trim().TrimEnd('&') + "&exit";

            string cmdFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "cmd.exe");
            using (Process p = new Process())
            {
                var result = new ExecResult();
                try
                {
                    p.StartInfo.FileName = cmdFileName;
                    p.StartInfo.UseShellExecute = false;        //�Ƿ�ʹ�ò���ϵͳshell����������Ϊfalse�����ض������������������ͬʱ��Ӱ��WorkingDirectory��ֵ
                    p.StartInfo.RedirectStandardInput = true;   //�������Ե��ó����������Ϣ
                    p.StartInfo.RedirectStandardOutput = true;  //�ɵ��ó����ȡ�����Ϣ
                    p.StartInfo.RedirectStandardError = true;   //�ض����׼�������
                    p.StartInfo.CreateNoWindow = true;          //����ʾ���򴰿�

                    if (!string.IsNullOrWhiteSpace(workDirectory))
                    {
                        p.StartInfo.WorkingDirectory = workDirectory;
                    }

                    p.Start();

                    p.StandardInput.WriteLine(command);
                    p.StandardInput.AutoFlush = true;

                    result.Output = p.StandardOutput.ReadToEnd();
                    result.Error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    p.Close();
                }
                catch (Exception ex)
                {
                    result.ExceptError = ex;
                }
                return result;
            }
        }

        #region �첽����
        /// <summary>
        /// ִ��cmd���� ����cmd������ʾ����Ϣ
        /// ��������ʹ���������������ӷ���
        /// <![CDATA[
        /// &:ͬʱִ����������
        /// |:����һ����������,��Ϊ��һ�����������
        /// &&����&&ǰ������ɹ�ʱ,��ִ��&&�������
        /// ||����||ǰ������ʧ��ʱ,��ִ��||�������]]>
        /// </summary>
        ///<param name="command">ִ�е�����</param>
        ///<param name="workDirectory">����Ŀ¼</param>
        /// <returns>cmd����ִ�д��ڵ����</returns>
        public static async Task<ExecResult> RunAsync(string command, string workDirectory = null)
        {

            command = command.Trim().TrimEnd('&') + "&exit";

            string cmdFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "cmd.exe");// @"C:\Windows\System32\cmd.exe";
            using (Process p = new Process())
            {
                var result = new ExecResult();
                try
                {
                    p.StartInfo.FileName = cmdFileName;
                    p.StartInfo.UseShellExecute = false;        //�Ƿ�ʹ�ò���ϵͳshell����
                    p.StartInfo.RedirectStandardInput = true;   //�������Ե��ó����������Ϣ
                    p.StartInfo.RedirectStandardOutput = true;  //�ɵ��ó����ȡ�����Ϣ
                    p.StartInfo.RedirectStandardError = true;   //�ض����׼�������
                    p.StartInfo.CreateNoWindow = true;          //����ʾ���򴰿�

                    if (!string.IsNullOrWhiteSpace(workDirectory))
                    {
                        p.StartInfo.WorkingDirectory = workDirectory;
                    }

                    p.Start();

                    //��cmd����д������
                    p.StandardInput.WriteLine(command);
                    p.StandardInput.AutoFlush = true;

                    // ��Ҫʹ��StandardError����������ProcessStartInfo.UseShellExecuteΪfalse�����ұ������� ProcessStartInfo.RedirectStandardError Ϊ true�� ���򣬴� StandardError ���ж�ȡ�������쳣��
                    //��ȡcmd�������Ϣ
                    result.Output = await p.StandardOutput.ReadToEndAsync();
                    result.Error = await p.StandardError.ReadToEndAsync();

                    p.WaitForExit();//�ȴ�����ִ�����˳����̡�Ӧ��������
                    p.Close();
                }
                catch (Exception ex)
                {
                    result.ExceptError = ex;
                }
                return result;
            }
        }

        /// <summary>
        /// ִ�ж��cmd���� ����cmd������ʾ����Ϣ
        /// �˴�ִ�еĶ���������ǽ���ִ�е���Ϣ���Ƕ������������Ҳ����ʹ��&���Ӷ�������Ϊһ��ִ��
        /// </summary>
        ///<param name="command">ִ�е�����</param>
        /// <returns>cmd����ִ�д��ڵ����</returns>
        /// <returns>����Ŀ¼</returns>
        public static async Task<ExecResult> RunAsync(string[] commands, string workDirectory = null)
        {
            if (commands == null)
            {
                throw new ArgumentNullException();
            }
            if (commands.Length == 0)
            {
                return default(ExecResult);
            }
            return await Task.Run(() =>
            {
                commands[commands.Length - 1] = commands[commands.Length - 1].Trim().TrimEnd('&') + "&exit";

                string cmdFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "cmd.exe");
                using (Process p = new Process())
                {
                    var result = new ExecResult();
                    try
                    {
                        p.StartInfo.FileName = cmdFileName;
                        p.StartInfo.UseShellExecute = false;        //�Ƿ�ʹ�ò���ϵͳshell����
                        p.StartInfo.RedirectStandardInput = true;   //�������Ե��ó����������Ϣ
                        p.StartInfo.RedirectStandardOutput = true;  //�ɵ��ó����ȡ�����Ϣ
                        p.StartInfo.RedirectStandardError = true;   //�ض����׼�������
                        p.StartInfo.CreateNoWindow = true;          //����ʾ���򴰿�

                        if (!string.IsNullOrWhiteSpace(workDirectory))
                        {
                            p.StartInfo.WorkingDirectory = workDirectory;
                        }

                        // ��������ķ�ʽ���ִ��ÿ��
                        var inputI = 1;
                        p.OutputDataReceived += (sender, e) =>
                        {
                            result.Output += $"{e.Data}{Environment.NewLine}";
                            if (inputI >= commands.Length)
                            {
                                return;
                            }
                            if (e.Data.Contains(commands[inputI - 1]))
                            {
                                p.StandardInput.WriteLine(commands[inputI]);
                            }
                            inputI++;
                        };

                        p.ErrorDataReceived += (sender, e) =>
                        {
                            result.Error += $"{e.Data}{Environment.NewLine}";
                            if (inputI >= commands.Length)
                            {
                                return;
                            }
                            if (e.Data.Contains(commands[inputI - 1]))
                            {
                                p.StandardInput.WriteLine(commands[inputI]);
                            }
                            inputI++;
                        };

                        p.Start();

                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.StandardInput.WriteLine(commands[0]);
                        p.StandardInput.AutoFlush = true;

                        p.WaitForExit();
                        p.Close();
                    }
                    catch (Exception ex)
                    {
                        result.ExceptError = ex;
                    }
                    return result;
                }
            });
        }
        #endregion
    }
}