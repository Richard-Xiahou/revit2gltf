using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf.utils
{
    /// <summary>
    /// 错误或警告信息处理
    /// </summary>
    public class FailurePreprocessor : IFailuresPreprocessor
    {
        public string ErrorMessage { set; get; }
        public string ErrorSeverity { set; get; }

        public FailurePreprocessor()
        {
            ErrorMessage = "";
            ErrorSeverity = "";
        }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor failureMessageAccessor in failureMessages)
            {
                FailureDefinitionId id = failureMessageAccessor.GetFailureDefinitionId();
                try
                {
                    // 获取错误的文字
                    ErrorMessage = failureMessageAccessor.GetDescriptionText();
                }
                catch
                {
                    ErrorMessage = "Unknown Error";
                }
                try
                {
                    // 是警告还是错误
                    FailureSeverity failureSeverity = failureMessageAccessor.GetSeverity();
                    ErrorSeverity = failureSeverity.ToString();
                    // 如果是警告，则禁止弹框
                    if (failureSeverity == FailureSeverity.Warning)
                    {
                        failureMessageAccessor.GetDefaultResolutionCaption();
                        failuresAccessor.DeleteWarning(failureMessageAccessor);
                    }
                    // 如果是错误，则尝试解决
                    if (failureSeverity == FailureSeverity.Error)
                    {
                        failuresAccessor.ResolveFailure(failureMessageAccessor);
                        failuresAccessor.DeleteWarning(failureMessageAccessor);
                        return FailureProcessingResult.ProceedWithRollBack; // 回滚了，不保留更改
                    }
                }
                catch (Exception)
                {
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
