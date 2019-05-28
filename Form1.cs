using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        string filePath = string.Empty;
        string pathToServer = string.Empty;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Button1_ClickAsync(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "wav files (*.wav)|*.wav";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;

                    var header = new WavHeader();
                    // Размер заголовка
                    var headerSize = Marshal.SizeOf(header);
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var buffer = new byte[headerSize];
                    fileStream.Read(buffer, 0, headerSize);

                    // Чтобы не считывать каждое значение заголовка по отдельности,
                    // воспользуемся выделением unmanaged блока памяти
                    var headerPtr = Marshal.AllocHGlobal(headerSize);
                    // Копируем считанные байты из файла в выделенный блок памяти
                    Marshal.Copy(buffer, 0, headerPtr, headerSize);
                    // Преобразовываем указатель на блок памяти к нашей структуре
                    Marshal.PtrToStructure(headerPtr, header);

                    Console.WriteLine("Sample rate: {0}", header.SampleRate);
                    Console.WriteLine("Channels: {0}", header.NumChannels);
                    Console.WriteLine("Bits per sample: {0}", header.BitsPerSample);

                    int sizeOfLayer = header.BitsPerSample * 1024;

                    var array = File.ReadAllBytes(filePath);

                    if (checkBox1.Checked == false)
                    {
                        int counter = 44;

                        byte[] sounds;
                        do
                        {
                            sounds = new byte[sizeOfLayer];
                            sounds = array.Skip(counter).Take(sizeOfLayer).ToArray();
                            SendWebReqestAsync(sounds);
                            counter += sizeOfLayer;
                        }
                        while (counter < array.Length);
                    }

                    //var result = await GetAnswer(pathToServer);

                    // Освобождаем выделенный блок памяти
                    Marshal.FreeHGlobal(headerPtr);
                }
            }
        }

        private async void SendWebReqestAsync(byte[] byteArray)
        {
            try
            {
                ByteArrayContent byteContent = new ByteArrayContent(byteArray);
                var client = new HttpClient();
                client.MaxResponseContentBufferSize = 256000;
                HttpResponseMessage reponse = await client.PostAsync(pathToServer, byteContent);

                if (reponse.IsSuccessStatusCode == true)
                    return;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private async Task<string> GetAnswer(string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url); 

            if (response.IsSuccessStatusCode == true)
            {
                string res = await response.Content.ReadAsStringAsync();

                return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(res);
            }
            else
            {
                return null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        // Структура, описывающая заголовок WAV файла.
        internal class WavHeader
        {
            // WAV-формат начинается с RIFF-заголовка:

            // Содержит символы "RIFF" в ASCII кодировке
            // (0x52494646 в big-endian представлении)
            public UInt32 ChunkId;

            // 36 + subchunk2Size, или более точно:
            // 4 + (8 + subchunk1Size) + (8 + subchunk2Size)
            // Это оставшийся размер цепочки, начиная с этой позиции.
            // Иначе говоря, это размер файла - 8, то есть,
            // исключены поля chunkId и chunkSize.
            public UInt32 ChunkSize;

            // Содержит символы "WAVE"
            // (0x57415645 в big-endian представлении)
            public UInt32 Format;

            // Формат "WAVE" состоит из двух подцепочек: "fmt " и "data":
            // Подцепочка "fmt " описывает формат звуковых данных:

            // Содержит символы "fmt "
            // (0x666d7420 в big-endian представлении)
            public UInt32 Subchunk1Id;

            // 16 для формата PCM.
            // Это оставшийся размер подцепочки, начиная с этой позиции.
            public UInt32 Subchunk1Size;

            // Аудио формат, полный список можно получить здесь http://audiocoding.ru/wav_formats.txt
            // Для PCM = 1 (то есть, Линейное квантование).
            // Значения, отличающиеся от 1, обозначают некоторый формат сжатия.
            public UInt16 AudioFormat;

            // Количество каналов. Моно = 1, Стерео = 2 и т.д.
            public UInt16 NumChannels;

            // Частота дискретизации. 8000 Гц, 44100 Гц и т.д.
            public UInt32 SampleRate;

            // sampleRate * numChannels * bitsPerSample/8
            public UInt32 ByteRate;

            // numChannels * bitsPerSample/8
            // Количество байт для одного сэмпла, включая все каналы.
            public UInt16 BlockAlign;

            // Так называемая "глубиная" или точность звучания. 8 бит, 16 бит и т.д.
            public UInt16 BitsPerSample;

            // Подцепочка "data" содержит аудио-данные и их размер.

            // Содержит символы "data"
            // (0x64617461 в big-endian представлении)
            public UInt32 Subchunk2Id;

            // numSamples * numChannels * bitsPerSample/8
            // Количество байт в области данных.
            public UInt32 Subchunk2Size;

            // Далее следуют непосредственно Wav данные.
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            pathToServer = textBox1.Text;
        }
    }
}
