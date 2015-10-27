using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Comsec.SqlPrune.Interfaces.Services.Providers;
using Sugar;

namespace Comsec.SqlPrune.Services.Providers
{
    /// <summary>
    /// Interface to wrap calls to the S3 service
    /// </summary>
    public class S3Provider : IFileProvider
    {
        /// <summary>
        /// Method called by the command to determine which <see cref="IFileProvider" /> implemetation should run.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool ShouldRun(string path)
        {
            return path != null && path.StartsWith("s3://", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Converts a region string to a region end point.
        /// </summary>
        /// <returns></returns>
        public RegionEndpoint GetRegion()
        {
            var region = Sugar.Command.Parameters.Current.AsString("region");

            if (string.IsNullOrEmpty(region))
            {
                region = ConfigurationManager.AppSettings["AWSDefaultRegionEndPoint"];
            }

            return RegionEndpoint.GetBySystemName(region);
        }

        /// <summary>
        /// Initalises an instance of the Amazon S3 client.
        /// </summary>
        /// <returns></returns>
        protected AmazonS3Client InitialiseClient()
        {
            var profileName = Sugar.Command.Parameters.Current.AsString("aws-profile");
            if (string.IsNullOrEmpty(profileName))
            {
                profileName = ConfigurationManager.AppSettings["AWSProfilesLocation"];
                if (string.IsNullOrEmpty(profileName))
                {
                    profileName = "default";
                }
            }

            var profileLocation = ConfigurationManager.AppSettings["AWSProfilesLocation"];
            if (string.IsNullOrEmpty(profileLocation))
            {
                profileLocation = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.aws\config";
            }

            var credentials = new StoredProfileAWSCredentials(profileName, profileLocation);

            var client = new AmazonS3Client(credentials, GetRegion());

            return client;
        }

        /// <summary>
        /// Extracts the bucket name from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public string ExtractBucketNameFromPath(string path)
        {
            return path.SubstringAfterChar("s3://").SubstringBeforeChar("/");
        }

        /// <summary>
        /// Extracts the filename from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public string ExtractFilenameFromPath(string path)
        {
            return path.SubstringAfterLastChar("/");
        }

        /// <summary>
        /// Determines whether the specified path is a directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool IsDirectory(string path)
        {
            var client = InitialiseClient();

            var request = new GetBucketLocationRequest
                          {
                              BucketName = ExtractBucketNameFromPath(path)
                          };

            try
            {
                // If the bucket location can be retrieved it's a valid bucket
                var response = client.GetBucketLocation(request);

                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.Message.Contains("does not exist"))
                {
                    return false;
                }

                throw ex;
            }
        }

        /// <summary>
        /// Gets the size of the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>-1 if the object doesn't exist</returns>
        public long GetFileSize(string path)
        {
            var bucketName = ExtractBucketNameFromPath(path);
            var subPath = path.SubstringAfterChar(bucketName + "/");

            var client = InitialiseClient();

            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = subPath
            };

            try
            {
                var response = client.GetObjectMetadata(request);

                return response == null
                    ? -1
                    : response.ContentLength;
            }
            catch(AmazonS3Exception ex)
            {
                if (ex.ErrorCode == "NotFound")
                {
                    return -1;
                }

                throw;
            }
        }

        /// <summary>
        /// Listings the objects.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="bucketName">Name of the bucket (e.g. "bucket-name").</param>
        /// <param name="prefix">The prefix (e.g. "/folder/filestart").</param>
        private static IList<S3Object> ListAllObjects(IAmazonS3 client, string bucketName, string prefix)
        {
            var objects = new List<S3Object>();

            try
            {
                var request = new ListObjectsRequest
                              {
                                  BucketName = bucketName,
                                  Prefix = prefix,
                                  MaxKeys = 1000
                              };

                do
                {
                    var response = client.ListObjects(request);

                    // Process response.
                    objects.AddRange(response.S3Objects);

                    // If response is truncated, set the marker to get the next set of keys.
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Check the provided AWS Credentials.");
                    Console.WriteLine("To sign up for service, go to http://aws.amazon.com/s3");
                }
                else
                {
                    Console.WriteLine("Error occurred. Message:'{0}' when listing objects", amazonS3Exception.Message);
                }
            }

            return objects;
        }

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified bucket + directory.
        /// </summary>
        /// <param name="dirPath">The bucket + directory to search.</param>
        /// <param name="searchPattern">The search patter (e.g. "*.txt").</param>
        /// <returns>
        /// A dictionary listing each file found and its size (in bytes).
        /// </returns>
        /// <exception cref="System.ArgumentException">Invalid search pattern: only '', '*', '*.ext' or 'name*' are supported</exception>
        /// <remarks>
        /// System Files and Folders will be ignored
        /// </remarks>
        public IDictionary<string, long> GetFiles(string dirPath, string searchPattern)
        {
            var bucketName = ExtractBucketNameFromPath(dirPath);
            var subPath = dirPath.SubstringAfterChar(bucketName + "/");

            if (subPath.Contains(bucketName))
            {
                subPath = null;
            }

            var client = InitialiseClient();

            var allObjectsInBucket = ListAllObjects(client, bucketName, subPath);

            var results = new Dictionary<string, long>(allObjectsInBucket.Count);

            IEnumerable<S3Object> matches;

            if (string.IsNullOrEmpty(searchPattern) || searchPattern == "*")
            {
                matches = allObjectsInBucket;
            }
            else if (searchPattern.Count(x => x == '*') == 1)
            {
                var pattern = searchPattern.Replace("*", "");

                matches = searchPattern.StartsWith("*")
                    ? allObjectsInBucket.Where(x => x.Key.EndsWith(pattern))
                    : allObjectsInBucket.Where(x => x.Key.StartsWith(pattern));
            }
            else
            {
                throw new ArgumentException("Invalid search pattern: only '', '*', '*.ext' or 'name*' are supported ", searchPattern);
            }

            foreach (var s3Object in matches)
            {
                var completePath = $"s3://{bucketName}/{s3Object.Key}";

                results.Add(completePath, s3Object.Size);
            }

            return results;
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The path (e.g. s3://bucket-name/path/to/file.extension).</param>
        public void Delete(string path)
        {
            var bucketName = ExtractBucketNameFromPath(path);
            var subPath = path.SubstringAfterChar(bucketName + "/");

            var client = InitialiseClient();

            var request = new DeleteObjectRequest
                          {
                              BucketName = bucketName,
                              Key = subPath
                          };

            client.DeleteObject(request);
        }

        /// <summary>
        /// Downloads the S3 object and saves it the specified  destination asynchronously.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <returns></returns>
        public async Task CopyToLocalAsync(string path, string destinationPath)
        {
            var bucketName = ExtractBucketNameFromPath(path);
            var subPath = path.SubstringAfterChar(bucketName + "/");

            var client = InitialiseClient();

            var utlity = new TransferUtility(client);

            var request = new TransferUtilityDownloadRequest
                          {
                              BucketName = bucketName,
                              Key = subPath,
                              FilePath = destinationPath
                          };

            await utlity.DownloadAsync(request);
        }
    }
}
