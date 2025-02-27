﻿/*
 * Copyright (c) 2023 Dialexio
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify,
 * merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using Claunia.PropertyList;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Octothorpe.Lib
{
    public class OTAPackage : IComparable<OTAPackage>
    {
        private Match match;
        private readonly Dictionary<string, object> ENTRY;
        private readonly NSDictionary BUILD_INFO_DICT = (NSDictionary)PropertyListParser.Parse(AppContext.BaseDirectory + Path.DirectorySeparatorChar + "BuildInfo.plist");

        public OTAPackage(Dictionary<string, object> package)
        {
            ENTRY = package;
        }

        public OTAPackage(JContainer package, string pallasDate)
        {
            Dictionary<string, object> convertedPackage = new Dictionary<string, object>();
            List<object> array;

            foreach (JProperty a in package)
            {
                // If we have an array, we need to go through each array item.
                if (a.Value.Type == JTokenType.Array)
                {
                    array = new List<object>();

                    foreach (JToken item in a.Value)
                        array.Add(item.ToObject<object>());

                    convertedPackage.Add(a.Name, array.ToArray());
                }

                else
                    convertedPackage.Add(a.Name, a.Value.ToObject<object>());
            }
            ENTRY = convertedPackage;
            // Add some missing values.
            ENTRY.Add("Date", pallasDate);
        }

        /// <summary>
        /// Returns the package's actual build number. (i.e. Without any of Apple's padding.)
        /// </summary>
        /// <returns>
        /// A string value of the package's actual build number. (e.g. 10A550 will be 10A550, 12F5061 will be 12F61)
        /// </returns>
        public string ActualBuild
        {
            get
            {
                // If the build number smells like a beta... We'll need to verify its actual build number.
                return (Regex.Match(DeclaredBuild, REGEX_BETA).Success) ?
                    RemoveBuildPadding(DeclaredBuild) :
                    DeclaredBuild;
            }
        }

        /// <summary>
        /// Returns the package's actual release type in the form of an integer.
        /// </summary>
        /// <returns>
        /// An integer value of 0 (not a beta), 1 (public beta), 2 (developer beta), 3 (carrier beta), or 4 (internal build). -1 may be returned if the type is unknown.
        /// </returns>
        public int ActualReleaseType
        {
            get
            {
                // Check ReleaseType and return values based on it.
                switch (ReleaseType)
                {
                    // We do need to dig deeper for betas though.
                    // We check if OTAPackage.DocumentationID says something.
                    // If that returns nothing, we check if the OTAPackage.DeclaredBuild
                    // looks like a beta build.
                    case "Beta":
                    case "Darwin":
                    case "Public":
                        if (DocumentationID.Contains("Public"))
                            return 1;

                        else if (Regex.IsMatch(DocumentationID.ToLower(), "(public|beta|seed)"))
                            return 2;

                        else if (Regex.IsMatch(DeclaredBuild, REGEX_BETA))
                            return 2;

                        else
                            return 0;

                    case "Carrier":
                        return 3;

                    case "Internal":
                        return 4;

                    default:
                        Console.WriteLine($"Unknown ReleaseType: {ENTRY["ReleaseType"]}");
                        return -1;
                }
            }
        }

        /// <summary>
        /// Some build numbers are small. This adds some padding to make it easier to sort.
        /// </summary>
        /// <returns>
        /// A string value of the provided build number, padded with zeroes for easier comparisons.
        /// </returns>
        private string AddBuildPadding(string BuildNum)
        {
            int LetterPos, NumPos;
            string zeros = null;

            for (LetterPos = 1; LetterPos < BuildNum.Length; LetterPos++)
            {
                if (char.IsUpper(BuildNum[LetterPos]))
                {
                    LetterPos++;
                    break;
                }
            }

            // NumPos will be the first actual digit that comes after the letter.
            NumPos = LetterPos + 1;

            while (BuildNum.Substring(NumPos).Length < 3)
                zeros.Insert(0, "0");

            return $"{BuildNum.Substring(0, LetterPos)}{zeros}{BuildNum.Substring(NumPos)}";
        }

        /// <summary>
        /// Reports the value of the key "AllowableOTA." If it's not present, returns true.
        /// </summary>
        /// <returns>
        /// A boolean value of whether this release is "allowable" (true) or not (false).
        /// </returns>
        public bool AllowableOTA
        {
            get
            {
                return ENTRY.TryGetValue("AllowableOTA", out object allowable) ?
                    (bool)allowable :
                    true;
            }
        }

        /// <summary>
        /// Reports the MobileAsset type.
        /// </summary>
        /// <returns>
        /// A string value of the MobileAsset type. It should be "com.apple.MobileAsset.SoftwareUpdate", "com.apple.MobileAsset.MacSoftwareUpdate", or "com.apple.MobileAsset.SplatSoftwareUpdate"
        /// </returns>
        public string AssetType
        {
            get
            {
                return ENTRY.TryGetValue("AssetType", out object assettype) ?
                    (string)assettype :
                    null;
            }
        }

        /// <summary>
        /// Reports the value of the key "AutoUpdate." If it's not present, returns false.
        /// </summary>
        /// <returns>
        /// A boolean value of whether the OS may auto-update to the package (true) or not (false).
        /// </returns>
        public bool AutoUpdate
        {
            get
            {
                return ENTRY.TryGetValue("AutoUpdate", out object autoupdate) ? (bool)autoupdate : false;
            }
        }

        /// <summary>
        /// Returns the beta number of this entry. If a beta number cannot be determined, returns 0.
        /// </summary>
        /// <returns>
        /// Whatever number beta this is, as an int.
        /// </returns>
        public int BetaNumber
        {
            get
            {
                if (GetKey("Beta") == null)
                {
                    string number = DocumentationID.Substring(DocumentationID.Length - 2);

                    if (Regex.IsMatch(DocumentationID.ToLower(), "(public|beta|seed)"))
                    {
                        if (char.IsDigit(number[0]))
                            return int.Parse(number);

                        else if (char.IsDigit(number[1]))
                            return (int)char.GetNumericValue(number[1]);

                        else
                            return 1;
                    }

                    else
                        return 0;
                }

                else
                {
                    return (int)GetKey("Beta");
                }
            }
        }
        
        public int CompareTo(OTAPackage compareWith)
        {
            return SortingString.CompareTo(compareWith.SortingString);
        }

        /// <summary>
        /// Returns the value in the entry's CompatibilityVersion key. If the compatibility version cannot be determined, this returns 0.
        /// </summary>
        /// <returns>
        /// Whatever is in the key CompatibilityVersion, as an int.
        /// </returns>
        public int CompatibilityVersion
        {
            get
            {
                return (ENTRY.TryGetValue("CompatibilityVersion", out object compatVer)) ?
                    Convert.ToInt32(compatVer) :
                    0;
            }
        }

        /// <summary>
        /// Returns the timestamp found in the URL. Note that this is not always accurate; it may be off by a day, or even a week.
        /// </summary>
        /// <returns>
        /// The timestamp found in the URL, which may not be accurate. Its format is YYYYMMDD.
        /// </returns>
        public string Date()
        {
            if (GetKey("Date") == null)
            {
                // Catch excess zeroes, e.g. "2016008004"
                match = Regex.Match(URL, @"\d{4}(\-|\.)20\d{8}\-");

                if (match.Success)
                    return match.ToString().Substring(5, 4) + match.ToString().Substring(10, 2) + match.ToString().Substring(13, 2);

                else
                {
                    match = Regex.Match(URL, @"\d{4}(\-|\.)20\d{6}(\-|.)");

                    if (match.Success)
                        return match.ToString().Substring(5, 8);

                    else
                        return "00000000";
                }
            }

            else
                return GetKey("Date").ToString();
        }

        /// <summary>
        /// Returns part of the timestamp found in the URL.
        /// </summary>
        /// <returns>
        /// The day, month, or year found in the URL.
        /// </returns>
        /// <param name="dmy">Indicates if you just want the day, month, or year.</param>
        public string Date(char dmy)
        {
            switch (dmy)
            {
                // Day
                case 'd':
                    return Date().Substring(6);

                // Month
                case 'm':
                    return Date().Substring(4, 2);

                // Year
                case 'y':
                    return Date().Substring(0, 4);

                default:
                    return Date();
            }
        }

        public string DeclaredBuild
        {
            get { return (string)ENTRY["Build"]; }
        }

        /// <summary>
        /// Reports the documentation ID that Apple assigned. This tells iOS which documentation file to load, since multiple ones may be offered.
        /// </summary>
        /// <returns>
        /// The documentation ID that corresponds to the OTA update. If one is not specified, returns "N/A."
        /// </returns>
        public string DocumentationID
        {
            get
            {
                return ENTRY.TryGetValue("SUDocumentationID", out object docid) ?
                    (string)docid :
                    "N/A";
            }
        }

        private object GetKey(string name)
        {
            NSDictionary ItemsForBuild;
            string osName = GetOSName();
            
            try
            {
                foreach (KeyValuePair<string, NSObject> majorVersion in (NSDictionary)BUILD_INFO_DICT[osName])
                {
                    if (((NSDictionary)majorVersion.Value).ContainsKey(ActualBuild))
                    {
                        ItemsForBuild = (NSDictionary)((NSDictionary)majorVersion.Value)[ActualBuild];

                        // We have the version in the key name now.
                        if (name == "Version")
                            return majorVersion.Key;

                        // If the item for that build specifies "Models," we need to check those out.
                        if (ItemsForBuild.ContainsKey("Models"))
                        {
                            foreach (NSObject Item in (NSArray)ItemsForBuild["Models"])
                            {
                                if (SupportedDeviceModels.Contains(Item.ToString()))
                                    return ItemsForBuild.ContainsKey(name) ? ItemsForBuild[name].ToObject() : null;
                            }

                            // Exception will only be thrown if the PLIST entry specifies "Models" but there isn't a match.
                            throw new KeyNotFoundException();
                        }

                        else
                            return ItemsForBuild.ContainsKey(name) ? ItemsForBuild[name].ToObject() : null;
                    }
                }

                return null;
            }

            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the release has an inflated build number. Apple does this to push devices on beta builds to stable builds.
        /// </summary>
        /// <returns>
        /// A boolean value of whether this release's reported build number is true or false.
        /// </returns>
        public bool IsHonestBuild
        {
            get { return ActualBuild == DeclaredBuild; }
        }

        /// <summary>
        /// Returns the value of "MarketingVersion" if present. "MarketingVersion" is used in some entries to display a false version number.
        /// </summary>
        /// <returns>
        /// A string value of the "MarketingVersion" key (if it exists), otherwise returns the "OSVersion" key.
        /// </returns>
        public string MarketingVersion
        {
            get
            {
                string mv = OSVersion;

                if (ENTRY.ContainsKey("MarketingVersion"))
                {
                    mv = (string)ENTRY["MarketingVersion"];

                    if (mv.Contains(".") == false)
                        mv += ".0";
                }

                if (string.IsNullOrEmpty(ProductVersionExtra) == false)
                    mv += $" {ProductVersionExtra}";

                return mv;
            }
        }

        /// <returns>
        /// The "_MaxOSVersion" key, as a string. If this key isn't present, returns "99.99" instead. This key is present for Apple Studio Display.
        /// </returns>

        public string MaxOSVersion
        {
            get
            {
                return (ENTRY.TryGetValue("_MaxOSVersion", out object version) ? (string)version : "99.99");
            }
        }

        /// <returns>
        /// The "_MinOSVersion" key, as a string. If this key isn't present, returns OTAPackage.PrerequisiteVer() instead. This key is present for Apple Studio Display.
        /// </returns>

        public string MinOSVersion
        {
            get
            {
                return (ENTRY.TryGetValue("_MinOSVersion", out object version) ? (string)version : PrerequisiteVer());
            }
        }

        /// <returns>
        /// The "OSVersion" key, as a string. For iOS 10 and newer, this will also strip "9.9." from the version.
        /// </returns>

        public string OSVersion
        {
            get
            {
                if (GetKey("Version") == null)
                {
                    string version = (string)ENTRY["OSVersion"];
                    return (version.Substring(0, 3) == "9.9") ? version.Substring(4) : version;
                }

                else
                    return (string)GetKey("Version");
            }
        }

        /// <summary>
        /// "PrerequisiteBuild" states the specific build that the OTA package is intended for, since most OTA packages are deltas.
        /// </summary>
        /// <returns>
        /// The "PrerequisiteBuild" key, as a string.
        /// </returns>
        public string PrerequisiteBuild
        {
            get
            {
                return (ENTRY.TryGetValue("PrerequisiteBuild", out object build) && Regex.Match((string)build, @"\d?\d[A-Z]\d(\d?){2}").Success) ?
                    (string)build :
                    "N/A";
            }
        }

        public string GetOSName()
        {
            string osName = null;
            // Items are separated by OS branch.
            if (AssetType == "com.apple.MobileAsset.MacSoftwareUpdate" || AssetType == "com.apple.MobileAsset.MacSplatSoftwareUpdate")
                osName = "macOS";

            else
            {
                switch (SupportedDevices[0].Substring(0, 3))
                {
                    // audioOS
                    case "Aud":
                        osName = "audioOS";
                        break;

                    // displayOS / tvOS
                    case "App":
                        osName = (SupportedDevices[0].Contains("Display") ? "displayOS" : "tvOS");
                        break;

                    // macOS
                    case "iMa":
                    case "Mac":
                        osName = "macOS";
                        break;

                    // iOS / iPadOS
                    case "iPa":
                    case "iPh":
                    case "iPo":
                        osName = "iOS";
                        break;

                    // visionOS
                    case "Rea":
                        osName = "visionOS";
                        break;

                    // watchOS
                    case "Wat":
                        osName = "watchOS";
                        break;
                }
            }

            return osName;
        }

        /// <summary>
        /// "PrerequisiteVer" states the specific version that the OTA package is intended for, since most OTA packages are deltas.
        /// </summary>
        /// <returns>
        /// If an entry is in BuildOverride.plist, that will be used to create a human-friendly string (e.g. "7.0 beta 5"). If not, returns the "PrerequisiteVersion" key as a string.
        /// </returns>
        public string PrerequisiteVer()
        {
            bool fuhgeddaboudit;
            int Beta = 0;
            NSDictionary ItemsForBuild = null;
            string osName = GetOSName();
            System.Text.StringBuilder VersionNum = new System.Text.StringBuilder();

            try
            {
                foreach (KeyValuePair<string, NSObject> majorVersion in (NSDictionary)BUILD_INFO_DICT[osName])
                {
                    ItemsForBuild = new NSDictionary();

                    if (((NSDictionary)majorVersion.Value).ContainsKey(PrerequisiteBuild))
                    {
                        ItemsForBuild = (NSDictionary)((NSDictionary)majorVersion.Value)[PrerequisiteBuild];
                        VersionNum.Append(majorVersion.Key);

                        break;
                    }
                }

                // Nothing found in BuildInfo.plist?
                if (ItemsForBuild.IsEmpty)
                    throw new KeyNotFoundException();

                if (ItemsForBuild.ContainsKey("Beta"))
                    Beta = (int)ItemsForBuild["Beta"].ToObject();

                if (ItemsForBuild.ContainsKey("Models"))
                {
                    fuhgeddaboudit = true;

                    foreach (NSObject model in (NSArray)ItemsForBuild["Models"])
                    {
                        if (SupportedDeviceModels.Contains(model.ToString()))
                        {
                            fuhgeddaboudit = false;
                            break;
                        }
                    }

                    if (fuhgeddaboudit)
                        throw new KeyNotFoundException();
                }

                if (Beta >= 1)
                {
                    VersionNum.Append(" beta");

                    if (Beta > 1)
                        VersionNum.Append($" {Beta}");
                }

                if (ItemsForBuild.ContainsKey("Suffix"))
                    VersionNum.Append($" {ItemsForBuild["Suffix"].ToString()}");

                return VersionNum.ToString();
            }

            catch (KeyNotFoundException)
            {
                return ENTRY.TryGetValue("PrerequisiteOSVersion", out object ver) ?
                    (string)ver :
                    "0.0";
            }
        }

        /// <summary>
        /// Specifies a suffix to append to the build number, commonly used with security response updates ("Splat Software Updates").
        /// </summary>
        /// <returns>
        /// The "ProductVersionExtra" key, as a string. If the key is not present, returns null.
        /// </returns>
        public string ProductVersionExtra
        {
            get
            {
                return ENTRY.TryGetValue("ProductVersionExtra", out object extra) ? (string)extra : null;
            }
        }

        /// <summary>
        /// This provides an easily accessible regular expression to detect build numbers that are (likely to be) beta versions.
        /// </summary>
        /// <returns>
        /// A regular expression, as a string, that can be used to detect build numbers belonging to beta versions.
        /// </returns>
        public static string REGEX_BETA
        {
            get { return @"\d?\d[A-Z][4-6]\d{3}[a-z]?"; }
        }

        /// <summary>
        /// Checks if Apple marked the OTA package with a release type. This function returns its value, but it may not be accurate. If accuracy is needed, use the ActualReleaseType() method.
        /// </summary>
        /// <returns>
        /// The "ReleaseType" key, as a string. If the key is not present, returns "Public."
        /// </returns>
        public string ReleaseType
        {
            get
            {
                return ENTRY.TryGetValue("ReleaseType", out object reltype) ? (string)reltype : "Public";
            }
        }

        /// <summary>
        /// Removes padding from a build number, supplied as a string argument.
        /// </summary>
        /// <returns>
        /// The build number, minus the padding for a beta.
        /// </returns>
        private string RemoveBuildPadding(string BuildNum)
        {
            if (Regex.Match(BuildNum, REGEX_BETA).Success)
            {
                int LetterPos, NumPos;

                for (LetterPos = 1; LetterPos < BuildNum.Length; LetterPos++)
                {
                    if (char.IsUpper(BuildNum[LetterPos]))
                    {
                        LetterPos++;
                        break;
                    }
                }

                // If the device is on its own branch, but is not a beta, there's no padding to strip. (e.g. 11.0.1 build 15A8391)
                if (BuildNum[LetterPos] == '8')
                    return BuildNum;

                // NumPos will be the position of the first actual digit that comes after the letter.
                NumPos = LetterPos + 1;

                // Do one last rudimentary check to make sure we don't accidentally trim a beta build number.
                // This currently checks if there are five characters after the first letter in the build number.
                if (BuildNum.Substring(LetterPos).Length == 5)
                {
                    // Apple likes to specify a fake 6 in the build number to target every beta with the OTA update. Let's (try to) fix that.
                    if (BuildNum[LetterPos] == '6' && char.IsLetter(DeclaredBuild[DeclaredBuild.Length - 1]))
                        return BuildNum.Substring(0, LetterPos) + '5' + BuildNum.Substring(NumPos);

                    // Return the beta build number.
                    return BuildNum;
                }

                // Checking on the number after the beta padding.
                // This handles padded numbers like 15B6092.
                if (BuildNum[NumPos] == '0')
                    NumPos++;

                return BuildNum.Substring(0, LetterPos) + BuildNum.Substring(NumPos);
            }

            else
                return BuildNum;
        }

        /// <summary>
        /// This is the size of the ZIP file.
        /// </summary>
        /// <returns>
        /// Returns the size of the OTA package as a number, formatted with commas.
        /// </returns>
        public string Size
        {
            get
            {
                if (ENTRY.TryGetValue("RealUpdateAttributes", out object RealUpdateAttrs))
                    return string.Format("{0:n0}", ((Dictionary<string, object>)RealUpdateAttrs)["RealUpdateDownloadSize"]);

                else
                    return string.Format("{0:n0}", ENTRY["_DownloadSize"]);
            }
        }

        private string SortingBuild()
        {
            int LetterPos;
            string BuildDigits, SortBuild;

            // Make 9A### appear before 10A###.
            SortBuild = (char.IsLetter(DeclaredBuild[1])) ? $"0{DeclaredBuild}" : DeclaredBuild;

            // Find the letter in the build number.
            for (LetterPos = 1; LetterPos < SortBuild.Length; LetterPos++)
            {
                if (char.IsUpper(SortBuild[LetterPos]))
                {
                    LetterPos++;
                    break;
                }
            }

            // If the build number is false, replace everything after the letter with "0000."
            // This will cause betas to appear first.
            if (IsHonestBuild == false)
                return $"{SortBuild.Substring(0, LetterPos)}0000";

            else
            {
                BuildDigits = SortBuild.Substring(LetterPos);

                if (char.IsLetter(BuildDigits[BuildDigits.Length - 1]))
                    BuildDigits = BuildDigits.Substring(0, BuildDigits.Length - 1);

                while (BuildDigits.Length < 4)
                    BuildDigits = $"0{BuildDigits}";

                return $"{SortBuild.Substring(0, LetterPos)}{BuildDigits}{SortBuild[SortBuild.Length-1]}";
            }
        }

        private string SortingPrerequisiteBuild()
        {
            // Sort by release type.
            string build = PrerequisiteBuild;

            // Skip everything if we know this is a universal build.
            if (PrerequisiteBuild == "N/A")
                switch (ReleaseType)
                {
                    //case "Beta":
                        //return "0000000001";

                    case "Carrier":
                        return "0000000002";

                    case "Internal":
                        return "0000000003";

                    default:
                        return "0000000000";
                }

            // Get up to (and including) the first letter. We'll get to this in a bit.
            match = Regex.Match(build, @"\d?\d[A-Z]");

            // If the build is old (i.e. before iOS 7), pad it.
            if (char.IsLetter(build[1]))
                return $"0{build}";
                
            if (PrerequisiteVer().Contains("beta") || PrerequisiteVer().Contains("RC"))
                build = RemoveBuildPadding(build);

            // If the number after the capital letter is too small, pad it.
            // (Note [A-Z] vs. [A-z]. The latter may chop off a lower case letter at the end.)
            while (new Regex("[A-z]").Split(build)[1].Length < 3 && match.Success)
                build = $"{match.Value}0{new Regex("[A-Z]").Split(build)[1]}";

            // Add a 6 to bump a public release's build number to be higher than beta build numbers.
            if ((new Regex("[A-z]").Split(build)[1]).Length == 3)
                build = $"{match.Value}6{new Regex("[A-Z]").Split(build)[1]}";

            // If the build does not have a letter, add a fake one to push it below similarly-numbered betas.
            if (char.IsDigit(build[build.Length - 1]))
                build = $"{build}z";

            return build;
        }

        /// <summary>
        /// This string is used for sorting purposes.
        /// </summary>
        /// <returns>
        /// Returns the values of ActualReleaseType(), a padded build, and a padded prerequisite build number in that order.
        /// </returns>
        public string SortingString
        {
            get
            {
                // If the prerequisite version is less than 10, pad it.
                string SortPreVer = (PrerequisiteVer().Split('.')[0].Length < 2) ? $"0{PrerequisiteVer()}" : PrerequisiteVer();

                return $"{SortingBuild()}${SortPreVer.Split(' ')[0]}${SortingPrerequisiteBuild()}${CompatibilityVersion}";
            }
        }

        /// <summary>
        /// The suffix for certain firmwares. If a firmware is a pre-release or a GM, it's suffixed appropriately.
        /// </summary>
        /// <returns>
        /// Returns a string defining the suffix for firmwares.
        /// </returns>
        public string Suffix
        {
            get
            {
                return (string)GetKey("Suffix");
            }
        }

        /// <summary>
        /// Contains a list of all supported models.
        /// </summary>
        /// <returns>
        /// A List of strings specifying what models are supported.
        /// </returns>
        public List<string> SupportedDeviceModels
        {
            get
            {
                List<string> Models = new List<string>();

                try
                {
                    Models = ((object[])ENTRY["SupportedDeviceModels"]).Select(i => (string)i).ToList();
                }

                // No models specified. (Very, very old PLISTs do this.)
                catch (KeyNotFoundException)
                {
                }

                return Models;
            }
        }

        /// <summary>
        /// Contains a list of all supported devices.
        /// </summary>
        /// <returns>
        /// A List of objects (which should be strings specifying what devices are supported).
        /// </returns>
        public List<string> SupportedDevices
        {
            get
            {
                return (ENTRY.ContainsKey("SupportedDevices")) ?
                    ((object[])ENTRY["SupportedDevices"]).Select(i => (string)i).ToList() :
                    new List<string>();
            }
        }

        /// <returns>
        /// The package URL, as a string.
        /// </returns>
        public string URL
        {
            get
            {
                if (ENTRY.TryGetValue("RealUpdateAttributes", out object RealUpdateAttrs))
                    return (string)((Dictionary<string, object>)RealUpdateAttrs)["RealUpdateURL"];

                else
                    return (string)ENTRY["__BaseURL"] + (string)ENTRY["__RelativePath"];
            }
        }
    }
}
