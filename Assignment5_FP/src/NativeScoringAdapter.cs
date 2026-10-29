using System;
using System.Runtime.InteropServices;

namespace ApplicationScoring
{
    public static class NativeScoringAdapter
    {
       
        [DllImport("native_scoring", CallingConvention = CallingConvention.Cdecl)]
        private static extern int calculate_experience_multiplier(int hours);

        
        public static Result<int, DomainError> GetExperienceMultiplier(int hours)
        {
            if (hours < 0)
            {
                return new Failure<int, DomainError>(new DomainError(ApplicationError.InvalidGpa, "Hours cannot be negative for native calculation."));
            }

            try
            {
                int result = calculate_experience_multiplier(hours);
                if (result < 0)
                {
                    return new Failure<int, DomainError>(new DomainError(ApplicationError.InvalidGpa, "Native calculation returned invalid state."));
                }
                return new Success<int, DomainError>(result);
            }
            catch (DllNotFoundException)
            {
                return new Failure<int, DomainError>(new DomainError(ApplicationError.FileNotFound, "Native library 'native_scoring' not found."));
            }
            catch (Exception ex)
            {
                return new Failure<int, DomainError>(new DomainError(ApplicationError.MalformedFile, $"Native call failed: {ex.Message}"));
            }
        }
    }
}