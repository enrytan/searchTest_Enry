using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Enry_searchTest.Models;

namespace Enry_searchTest.Controllers
{
    public class SearchController : Controller
    {
        public const string folderPath = "~/App_Data/";
        public const string logFileName = "logFile.csv";

        // GET: Search
        [HttpGet]
        public ActionResult SearchData()
        {
            var data = new DatasetModel();
            return View(data);
        }

        public ActionResult InsertData()
        {
            string logFilePath = Path.Combine(Server.MapPath(folderPath), logFileName);


            DeletePreviousInputFile();

            var generatedFileName = GenerateFile();
            if (generatedFileName != "")
            {
                if (System.IO.File.Exists(logFilePath))
                {
                    System.IO.File.Delete(logFilePath);
                }

                using (StreamWriter swriter = System.IO.File.AppendText(logFilePath))
                {
                    swriter.WriteLine(generatedFileName);
                    swriter.Close();
                }

                TempData["DataStatus"] = "Data created!";
            }
            else
            {
                TempData["DataStatus"] = "Insert data failed!";
            }
            return RedirectToAction("SearchData");
        }

        // POST: SearchData
        [HttpPost]
        public ActionResult SearchData(DatasetModel data)
        {
            if (data.keyword != null)
            {
                string logFilePath = Path.Combine(Server.MapPath(folderPath), logFileName);
                var dataFilename = "";

                // find the latest data from logFile
                using (var sreader = new StreamReader(logFilePath))
                {
                    dataFilename = sreader.ReadLine();
                    sreader.Close();
                }

                string savePath = Path.Combine(Server.MapPath(folderPath), dataFilename);
                var dataset = new List<DataModel>();

                // read and compare the data with the keyword
                using (var sreader = new StreamReader(savePath))
                {
                    while (!sreader.EndOfStream)
                    {
                        string[] rows = sreader.ReadLine().Split(',');
                        if (FindMatch(rows[1].ToString(), data.keyword))
                        {
                            // if match, store to a new list
                            dataset.Add(new DataModel
                            {
                                stringId = Guid.Parse(rows[0].ToString()),
                                stringContent = rows[1].ToString()
                            });
                        }
                    }
                }

                // extract stringId and add match count
                var matchedStringId = (from d in dataset
                                       select new DataModel
                                       {
                                           stringId = d.stringId,
                                           matchCount = 1
                                       }).ToList();
                // update the match count and return totalCount
                var newLogData = UpdateMatchCount(matchedStringId);

                // assign the new matchCount to the current list
                foreach (var item in dataset)
                {
                    item.matchCount = (from a in newLogData
                                       where a.stringId == item.stringId
                                       select a.matchCount).First();
                }

                data.dataset = dataset;
            }
            return View(data);
        }

        #region helpers

        public string GenerateFile()
        {
            var status = false;
            // set to generate 100k records
            var row = 100000;

            var guidFileName = Guid.NewGuid().ToString() + "_v5.csv";
            string savePath = Path.Combine(Server.MapPath(folderPath), guidFileName);

            Random r = new Random();

            // random from alphanumeric including space
            const string charList = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz 0123456789";

            using (StreamWriter swriter = System.IO.File.AppendText(savePath))
            {
                for (var h = 0; h < row; h++)
                {
                    // set stringContent length between 1k to 2k bytes
                    int randomLength = r.Next(1024, 2048);

                    var finalString = new StringBuilder();
                    var stringContent = new char[randomLength];
                    var stringId = Guid.NewGuid().ToString();
                    finalString.Append(stringId + ",");

                    for (var i = 0; i < randomLength; i++)
                    {
                        stringContent[i] = charList[r.Next(charList.Length)];
                    }
                    finalString.Append(stringContent);
                    swriter.WriteLine(finalString);
                }

                swriter.Close();
                status = true;
            }

            if (status)
            {
                return guidFileName;
            }
            else
            {
                return string.Empty;
            }
        }

        public void DeletePreviousInputFile()
        {
            string logFilePath = Path.Combine(Server.MapPath(folderPath), logFileName);
            string previousFileName = "";

            //get previous dataFile name from logFile then delete
            if (System.IO.File.Exists(logFilePath))
            {
                using (var sreader = new StreamReader(logFilePath))
                {
                    previousFileName = sreader.ReadLine().ToString();
                    sreader.Close();
                }

                var toDeletePath = Path.Combine(Server.MapPath(folderPath), previousFileName);
                if (System.IO.File.Exists(toDeletePath))
                {
                    System.IO.File.Delete(toDeletePath);
                }
            }
        }

        public bool FindMatch(string word, string keyword)
        {
            var isMatch = false;
            var stringFound = "";

            var wordsArr = word.ToUpper().ToCharArray();
            var keywordArr = keyword.ToUpper().ToCharArray();

            var wordArrayPos = 0;

            for (var keywordArrayPos = 0; keywordArrayPos < keywordArr.Length; keywordArrayPos++)
            {
                while (wordArrayPos < wordsArr.Length)
                {
                    if (keywordArr[keywordArrayPos] == wordsArr[wordArrayPos])
                    {
                        stringFound += wordsArr[wordArrayPos];
                        isMatch = true;
                        wordArrayPos++;
                        break;
                    }
                    else
                    {
                        if (isMatch == true)
                        {
                            isMatch = false;
                            keywordArrayPos = 0;
                            stringFound = "";
                        }
                        else
                        {
                            isMatch = false;
                            wordArrayPos++;
                        }
                    }
                }
            }

            if (keyword.ToUpper() != stringFound)
            {
                isMatch = false;
            }

            return isMatch;
        }

        public List<DataModel> UpdateMatchCount(List<DataModel> matchedRecords)
        {
            // new list for the modified logData
            var updatedLogData = new List<DataModel>();

            if (matchedRecords.Count > 0)
            {
                string logFilePath = Path.Combine(Server.MapPath(folderPath), logFileName);
                string dataFilename = "";

                // list for storing the current logData
                var logData = new List<DataModel>();
                using (var sreader = new StreamReader(logFilePath))
                {
                    dataFilename = sreader.ReadLine();
                    while (!sreader.EndOfStream)
                    {
                        string[] rows = sreader.ReadLine().Split(',');
                        logData.Add(new DataModel
                        {
                            stringId = Guid.Parse(rows[0].ToString()),
                            matchCount = Convert.ToInt32(rows[1])
                        });
                    }
                    sreader.Close();
                }

                // compare the matched stringId with the current logData
                foreach (var item in matchedRecords.ToList())
                {
                    for (var i = 0; i < logData.Count(); i++)
                    {
                        if (item.stringId == logData[i].stringId)
                        {
                            //if match, count+1, write to new logData list, and remove both item from list
                            updatedLogData.Add(new DataModel
                            {
                                stringId = logData[i].stringId,
                                matchCount = logData[i].matchCount + item.matchCount
                            });
                            logData.RemoveAt(i);
                            matchedRecords.Remove(item);
                            break;
                        }
                    }
                }

                // write the current logData and matched data to new list
                updatedLogData.AddRange(logData);
                updatedLogData.AddRange(matchedRecords);
                
                // delete the current logData, then create new logData with new list
                if (System.IO.File.Exists(logFilePath))
                {
                    System.IO.File.Delete(logFilePath);
                }
                using (StreamWriter swriter = System.IO.File.AppendText(logFilePath))
                {
                    swriter.WriteLine(dataFilename);
                    foreach (var item in updatedLogData)
                    {
                        swriter.WriteLine(item.stringId + "," + item.matchCount);
                    }

                    swriter.Close();
                }
            }

            return updatedLogData;
        }


        #endregion
    }
}
