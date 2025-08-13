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
        private readonly object _lock = new object();

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
        /// STX/ETX 기반으로 완전한 메시지 파싱
        /// </summary>
        public void ParseAndEnqueue(byte[] rawData)
        {
            var dataString = System.Text.Encoding.UTF8.GetString(rawData);
            var messages = ParseMessages(dataString);
            
            foreach (var message in messages)
            {
                Enqueue(message);
            }
        }

        /// <summary>
        /// STX(0x02)와 ETX(0x03) 사이의 메시지 추출
        /// </summary>
        private List<string> ParseMessages(string data)
        {
            var messages = new List<string>();
            const char STX = (char)0x02;
            const char ETX = (char)0x03;

            int startIndex = 0;
            while (startIndex < data.Length)
            {
                int stxIndex = data.IndexOf(STX, startIndex);
                if (stxIndex == -1) break;

                int etxIndex = data.IndexOf(ETX, stxIndex + 1);
                if (etxIndex == -1) break;

                // STX와 ETX 사이의 데이터 추출
                string message = data.Substring(stxIndex + 1, etxIndex - stxIndex - 1);
                if (!string.IsNullOrEmpty(message))
                {
                    messages.Add(message);
                }

                startIndex = etxIndex + 1;
            }

            return messages;
        }
    }
}
