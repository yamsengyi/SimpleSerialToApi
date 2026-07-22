using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// STX/ETX 기반 시리얼 데이터를 위한 간단한 큐 서비스
    /// </summary>
    public class SimpleQueueService
    {
        private readonly ConcurrentQueue<string> _queue = new();
        private readonly List<byte> _frameBuffer = new();
        private readonly object _frameLock = new object();

        /// <summary>
        /// 큐에 데이터 추가
        /// </summary>
        public void Enqueue(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                _queue.Enqueue(data);
            }
        }

        /// <summary>
        /// 큐에서 데이터 가져오기
        /// </summary>
        public bool TryDequeue(out string? data)
        {
            return _queue.TryDequeue(out data);
        }

        /// <summary>
        /// 큐의 모든 데이터 가져오고 비우기
        /// </summary>
        public List<string> DequeueAll()
        {
            var messages = new List<string>();
            
            while (_queue.TryDequeue(out var message))
            {
                messages.Add(message);
            }
            
            return messages;
        }

        /// <summary>
        /// 큐 크기
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 큐가 비어있는지 확인
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// 큐의 모든 데이터를 삭제
        /// </summary>
        public void ClearQueue()
        {
            // ConcurrentQueue는 Clear 메서드가 없으므로 모든 데이터를 빼내어 삭제
            while (_queue.TryDequeue(out _))
            {
                // 모든 항목을 제거
            }
        }

        /// <summary>
        /// STX/ETX 기반으로 완전한 메시지 파싱
        /// </summary>
        public void ParseAndEnqueue(byte[] rawData)
        {
            foreach (var message in ExtractMessages(rawData))
            {
                Enqueue(message);
            }
        }

        /// <summary>
        /// 수신 이벤트 경계를 넘어 STX(0x02)와 ETX(0x03) 사이의 완전한 메시지를 추출합니다.
        /// </summary>
        public IReadOnlyList<string> ExtractMessages(byte[] rawData)
        {
            var messages = new List<string>();
            if (rawData == null || rawData.Length == 0)
            {
                return messages;
            }

            lock (_frameLock)
            {
                _frameBuffer.AddRange(rawData);

                while (true)
                {
                    var stxIndex = _frameBuffer.IndexOf(0x02);
                    if (stxIndex < 0)
                    {
                        _frameBuffer.Clear();
                        break;
                    }

                    if (stxIndex > 0)
                    {
                        _frameBuffer.RemoveRange(0, stxIndex);
                    }

                    var etxIndex = _frameBuffer.IndexOf(0x03, 1);
                    if (etxIndex < 0)
                    {
                        break;
                    }

                    if (etxIndex > 1)
                    {
                        messages.Add(System.Text.Encoding.UTF8.GetString(_frameBuffer.GetRange(1, etxIndex - 1).ToArray()));
                    }

                    _frameBuffer.RemoveRange(0, etxIndex + 1);
                }
            }

            return messages;
        }

        public bool IsFrameInProgress
        {
            get
            {
                lock (_frameLock)
                {
                    return _frameBuffer.Count > 0;
                }
            }
        }
    }
}
