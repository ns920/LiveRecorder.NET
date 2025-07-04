using AcfunApi;
using LiveRecorder.NET.Data;
using LiveRecorder.NET.Models.Acfun;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Acfun.DataBase
{
    public partial class Form1 : Form
    {
        private AcfunlivedbDbContext _dbContext;
        private AcfunApiRequest _acfunApiRequest;

        public Form1()
        {
            InitializeComponent();

            try
            {
                InitializeDatabaseAsync();
                InitializeListView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��ʼ��ʧ�ܣ�{ex.Message}\n\n��ϸ��Ϣ��{ex}", "����",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                _dbContext = new AcfunlivedbDbContext();

                // �첽��ȡ��¼��Ϣ
                var loginInfo = await AcfunApiRequest.GetLoginInformation();

                // ʹ�õ�¼��Ϣʵ���� AcfunApiRequest
                _acfunApiRequest = new AcfunApiRequest(loginInfo);

                // �������ݿ�����
                _dbContext.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"���ݿ��ʼ��ʧ��: {ex.Message}", "����",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeListView()
        {
            // ����ListView����ʾģʽ
            listView_Data.View = View.Details;
            listView_Data.FullRowSelect = true;
            listView_Data.GridLines = true;

            // ����б���
            listView_Data.Columns.Add("ֱ��ID", 100);
            listView_Data.Columns.Add("UP��UID", 80);
            listView_Data.Columns.Add("UP���ǳ�", 120);
            listView_Data.Columns.Add("ֱ������", 200);
            listView_Data.Columns.Add("��ʼʱ��", 150);
            listView_Data.Columns.Add("¼������", 300);
            listView_Data.Columns.Add("����¼������", 300);

            // ����Ҽ��˵����ڸ���
            var contextMenu = new ContextMenuStrip();

            var copyUrlMenuItem = new ToolStripMenuItem("����¼������");
            copyUrlMenuItem.Click += (s, e) => CopyRecordingUrl();
            contextMenu.Items.Add(copyUrlMenuItem);

            var copyBackupUrlMenuItem = new ToolStripMenuItem("���Ʊ�������");
            copyBackupUrlMenuItem.Click += (s, e) => CopyBackupUrl();
            contextMenu.Items.Add(copyBackupUrlMenuItem);


            listView_Data.ContextMenuStrip = contextMenu;
        }

        // ��дProcessCmdKey�����Բ���Ctrl+C��ݼ�
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ����Ƿ�����Ctrl+C
            if (keyData == (Keys.Control | Keys.C))
            {
                // ��鵱ǰ�����Ƿ���ListView������ѡ����
                if (listView_Data.Focused && listView_Data.SelectedItems.Count > 0)
                {
                    CopyRecordingUrl();
                    return true; // ��ʾ�Ѵ���ð���
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CopyRecordingUrl()
        {
            if (listView_Data.SelectedItems.Count > 0)
            {
                var selectedItem = listView_Data.SelectedItems[0];
                var selectedLive = (AcfunLive)selectedItem.Tag;

                if (!string.IsNullOrEmpty(selectedLive.url))
                {
                    Clipboard.SetText(selectedLive.url);
                }
            }
        }

        private void CopyBackupUrl()
        {
            if (listView_Data.SelectedItems.Count > 0)
            {
                var selectedItem = listView_Data.SelectedItems[0];
                var selectedLive = (AcfunLive)selectedItem.Tag;

                if (!string.IsNullOrEmpty(selectedLive.url_backup))
                {
                    Clipboard.SetText(selectedLive.url_backup);
                }
            }
        }

        private async void button_Search_Click(object sender, EventArgs e)
        {
            try
            {
                button_Search.Enabled = false;
                button_Search.Text = "��ѯ��...";

                listView_Data.Items.Clear();

                string searchText = textBox_Search.Text.Trim();
                List<AcfunLive> searchResults;

                if (string.IsNullOrEmpty(searchText))
                {
                    // ���������Ϊ�գ���ʾ���100����¼
                    searchResults = await _dbContext.Lives
                        .OrderByDescending(x => x.startTime)
                        .Take(100)
                        .ToListAsync();
                }
                else
                {
                    // ��������������ѯ
                    if (int.TryParse(searchText, out int uid))
                    {
                        // �������������֣���UID����
                        searchResults = await _dbContext.Lives
                            .Where(x => x.uid == uid)
                            .OrderByDescending(x => x.startTime)
                            .ToListAsync();
                    }
                    else
                    {
                        // �����������ı������ǳƻ��������
                        searchResults = await _dbContext.Lives
                            .Where(x => x.name.Contains(searchText) || x.title.Contains(searchText))
                            .OrderByDescending(x => x.startTime)
                            .ToListAsync();
                    }
                }

                // �����������ʾ��ListView
                foreach (var live in searchResults)
                {
                    var item = new ListViewItem(live.liveId);
                    item.SubItems.Add(live.uid.ToString());
                    item.SubItems.Add(live.name);
                    item.SubItems.Add(live.title);
                    item.SubItems.Add(DateTimeOffset.FromUnixTimeMilliseconds(live.startTime).ToString("yyyy-MM-dd HH:mm:ss"));
                    // ��ʾ¼�����ӵ�״̬���������������ӣ�����������ӵ����
                    item.SubItems.Add(live.url);
                    item.SubItems.Add(live.url_backup);
                    item.Tag = live; // �洢�������󹩺���ʹ��
                    listView_Data.Items.Add(item);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"��ѯʧ�ܣ�{ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button_Search.Enabled = true;
                button_Search.Text = "��ѯ";
            }
        }

        private async void button_GetUrl_Click(object sender, EventArgs e)
        {
            if (listView_Data.SelectedItems.Count == 0)
            {
                MessageBox.Show("����ѡ��һ��ֱ����¼", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                button_GetUrl.Enabled = false;
                button_GetUrl.Text = "��ȡ��...";

                var selectedItem = listView_Data.SelectedItems[0];
                var selectedLive = (AcfunLive)selectedItem.Tag;

                if (!string.IsNullOrEmpty(selectedLive.url))
                {
                    return;
                }

                // ����API��ȡ¼������
                var form = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("liveId", selectedLive.liveId)
                };

                var requestUrl = "https://api.kuaishouzt.com/rest/zt/live/playBack/startPlay?subBiz=mainApp&kpn=ACFUN_APP&kpf=PC_WEB";
                var response = await _acfunApiRequest.Post(requestUrl, form);

                var result = JsonConvert.DeserializeObject<JToken>(response)?["result"]?.ToString();
                if (result is null || result != "1")
                {
                    MessageBox.Show("��ȡ¼������ʧ�ܣ�API���ش���", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var responseObject = JsonConvert.DeserializeObject<GetPlaybackResponse>(response);
                var adaptiveManifest = JsonConvert.DeserializeObject<AdaptiveManifest>(responseObject.data.adaptiveManifest);

                // ��ȡ¼�����Ӳ��������ݿ�
                if (adaptiveManifest.adaptationSet.Any() &&
                    adaptiveManifest.adaptationSet[0].representation.Any())
                {
                    // ��ȡ��¼������
                    var mainUrl = adaptiveManifest.adaptationSet[0].representation[0].url;
                    selectedLive.url = mainUrl;

                    // ��ȡ����¼�����ӣ�����У�
                    if (adaptiveManifest.adaptationSet[0].representation[0].backupUrl.Any())
                    {
                        selectedLive.url_backup = adaptiveManifest.adaptationSet[0].representation[0].backupUrl[0];
                    }

                    // ���浽���ݿ�
                    _dbContext.Update(selectedLive);
                    await _dbContext.SaveChangesAsync();

                    // ����ListView��ʾ
                    button_Search_Click(null, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("δ�ҵ���Ч��¼������", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��ȡ¼������ʧ�ܣ�{ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button_GetUrl.Enabled = true;
                button_GetUrl.Text = "��ȡ����";
            }
        }
    }
}
