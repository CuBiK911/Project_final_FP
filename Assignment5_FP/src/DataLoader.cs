using System;
using System.Collections.Generic;
using System.IO;

namespace ApplicationScoring
{
    public static class DataLoader
    {
     
        public static int TransformationCount { get; private set; } = 0;

        public static void ResetCounter() => TransformationCount = 0;

    
        public static IEnumerable<RawApplication> LoadAndStreamRawApplications(string filePath)
        {
            string[] lines;
            
         
            using (var reader = new StreamReader(filePath)) 
            {
                lines = reader.ReadToEnd().Split('\n');
            } 

            for (int i = 1; i < lines.Length; i++) 
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                var parts = line.Split(',');
                if (parts.Length == 5)
                {
                    TransformationCount++; 
                    yield return new RawApplication(parts[0], parts[1], parts[2], parts[3], parts[4]);
                }
            }
        }
    }
}