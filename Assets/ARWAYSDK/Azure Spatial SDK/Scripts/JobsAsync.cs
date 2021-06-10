/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Text;
using UnityEngine;
using System.Net;

namespace Arway
{
    public class JobAsync
    {
        public Action OnStart;
        public Action<string> OnError;
        public Progress<float> Progress = new Progress<float>();

        public virtual async Task RunJobAsync()
        {
            await Task.Yield();
        }

        protected void HandleError(string e)
        {
            OnError?.Invoke(e ?? "conn");
        }
    }
    //============================================================================================================
    //  -------------------------------------     JobMapUploadAsync      ------------------------------------
    //============================================================================================================

    public class JobMapUploadAsync : JobAsync
    {
        public string mapName;
        public string devToken;

        public string latitude;
        public string longitude;
        public string altitude;

        public string plyPath;

        public string version;
        public string anchorId;

        public Action<string> OnResult;

        public override async Task RunJobAsync()
        {
            Debug.Log("******************   Map Upload Job  ******************");

            this.OnStart?.Invoke();

            MapRequest mapRequest = new MapRequest
            {
                devToken = this.devToken,
                map_name = this.mapName,

                latitude = this.latitude,
                longitude = this.longitude,
                altitude = this.altitude,

                pcdPath = this.plyPath,
                version = this.version,
                anchorId = this.anchorId
            };

            string result = await ArwayHttp.RequestMapUpload<MapRequest, string>(mapRequest, this.Progress);

            Debug.Log("result: " + result);

            if (result.Length > 0)
            {
                Debug.Log("OnResult>>>>Invoke");
                this.OnResult?.Invoke(result);
            }
            else
            {
                HandleError(result);
                Debug.Log("Error: >>>" + result);
            }
        }
    }

    //============================================================================================================
    //  ------------------------------------------     ArwayHttp      ------------------------------------------
    //============================================================================================================

    public class ArwayHttp
    {
        // map upload request
        public static async Task<string> RequestMapUpload<T, U>(T request, IProgress<float> progress)
        {
            string result = "";

            string jsonString = JsonUtility.ToJson(request);
            MapRequest mapRequest = JsonUtility.FromJson<MapRequest>(jsonString);

            string mapName = mapRequest.map_name;
            string m_longitude = mapRequest.latitude;
            string m_latitude = mapRequest.longitude;
            string m_altitude = mapRequest.altitude;
            string devToken = mapRequest.devToken;

            string plyPath = mapRequest.pcdPath;
            string sdkVersion = mapRequest.version;
            string anchorId = mapRequest.anchorId;

            var multiForm = new MultipartFormDataContent();
            multiForm.Add(new StringContent(mapName), "map_name");
            multiForm.Add(new StringContent(m_latitude), "Latitude");
            multiForm.Add(new StringContent(m_longitude), "Longitude");
            multiForm.Add(new StringContent(m_altitude), "Altitude");

            multiForm.Add(new StringContent(sdkVersion), "version");

            multiForm.Add(new StringContent(anchorId), "anchor_id");

            FileStream ply = File.OpenRead(plyPath);
            multiForm.Add(new StreamContent(ply), "pcd", Path.GetFileName(plyPath));

            HttpRequestMessage requestBody = new HttpRequestMessage(HttpMethod.Post, ArwaySDK.arwayServerRootUrl + EndPoint.MAP_UPLOAD);

            // Lets keep buffer of 80kb  = 20 * 4096;
            var progressContent = new ProgressableStream(multiForm, 20 * 4096, (sent, total) =>
              {
                  progress?.Report((float)sent / total);
              });

            requestBody.Content = progressContent;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (var response = await UploadManager.mapperClient.ApiCallAsync(requestBody, stream, null, CancellationToken.None))
                    {
                        string responseBody = Encoding.ASCII.GetString(stream.GetBuffer());
                        result = responseBody;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ArwayHttp connection error: " + e);
            }
            return result;
        }


    }


    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> ApiCallAsync(this HttpClient client, HttpRequestMessage request, Stream destination, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {

            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                request.Dispose();

                var contentLength = response.Content.Headers.ContentLength;

                using (var download = await response.Content.ReadAsStreamAsync())
                {
                    if (progress == null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return response;
                    }

                    var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
                    await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
                }

                return response;
            }
        }
    }

    public class ProgressableStream : HttpContent
    {

        private HttpContent content;
        private int bufferSize;
        private Action<long, long> progress;

        public ProgressableStream(HttpContent content, int bufferSize, Action<long, long> progress)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this.content = content;
            this.bufferSize = bufferSize;
            this.progress = progress;

            foreach (var h in content.Headers)
            {
                this.Headers.Add(h.Key, h.Value);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {

            return Task.Run(async () =>
            {
                var buffer = new Byte[this.bufferSize];
                long size;
                TryComputeLength(out size);
                var uploaded = 0;


                using (var sinput = await content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        var length = sinput.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        uploaded += length;
                        progress?.Invoke(uploaded, size);

                        stream.Write(buffer, 0, length);
                        stream.Flush();
                    }
                }
                stream.Flush();
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Headers.ContentLength.GetValueOrDefault();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }
            base.Dispose(disposing);
        }

    }

    public static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new ArgumentException("Has to be readable", nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new ArgumentException("Has to be writable", nameof(destination));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }

}
