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
                MessageBox.Show($"初始化失败：{ex.Message}\n\n详细信息：{ex}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                _dbContext = new AcfunlivedbDbContext();

                // 异步获取登录信息
                var loginInfo = await AcfunApiRequest.GetLoginInformation();

                // 使用登录信息实例化 AcfunApiRequest
                _acfunApiRequest = new AcfunApiRequest(loginInfo);

                // 测试数据库连接
                _dbContext.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeListView()
        {
            // 设置ListView的显示模式
            listView_Data.View = View.Details;
            listView_Data.FullRowSelect = true;
            listView_Data.GridLines = true;

            // 添加列标题
            listView_Data.Columns.Add("直播ID", 100);
            listView_Data.Columns.Add("UP主UID", 80);
            listView_Data.Columns.Add("UP主昵称", 120);
            listView_Data.Columns.Add("直播标题", 200);
            listView_Data.Columns.Add("开始时间", 150);
            listView_Data.Columns.Add("录像链接", 300);
            listView_Data.Columns.Add("备用录像链接", 300);

            // 添加右键菜单用于复制
            var contextMenu = new ContextMenuStrip();

            var copyUrlMenuItem = new ToolStripMenuItem("复制录像链接");
            copyUrlMenuItem.Click += (s, e) => CopyRecordingUrl();
            contextMenu.Items.Add(copyUrlMenuItem);

            var copyBackupUrlMenuItem = new ToolStripMenuItem("复制备用链接");
            copyBackupUrlMenuItem.Click += (s, e) => CopyBackupUrl();
            contextMenu.Items.Add(copyBackupUrlMenuItem);


            listView_Data.ContextMenuStrip = contextMenu;
        }

        // 重写ProcessCmdKey方法以捕获Ctrl+C快捷键
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 检查是否按下了Ctrl+C
            if (keyData == (Keys.Control | Keys.C))
            {
                // 检查当前焦点是否在ListView上且有选中项
                if (listView_Data.Focused && listView_Data.SelectedItems.Count > 0)
                {
                    CopyRecordingUrl();
                    return true; // 表示已处理该按键
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
                button_Search.Text = "查询中...";

                listView_Data.Items.Clear();

                string searchText = textBox_Search.Text.Trim();
                List<AcfunLive> searchResults;

                if (string.IsNullOrEmpty(searchText))
                {
                    // 如果搜索框为空，显示最近100条记录
                    searchResults = await _dbContext.Lives
                        .OrderByDescending(x => x.startTime)
                        .Take(100)
                        .ToListAsync();
                }
                else
                {
                    // 根据搜索条件查询
                    if (int.TryParse(searchText, out int uid))
                    {
                        // 如果输入的是数字，按UID搜索
                        searchResults = await _dbContext.Lives
                            .Where(x => x.uid == uid)
                            .OrderByDescending(x => x.startTime)
                            .ToListAsync();
                    }
                    else
                    {
                        // 如果输入的是文本，按昵称或标题搜索
                        searchResults = await _dbContext.Lives
                            .Where(x => x.name.Contains(searchText) || x.title.Contains(searchText))
                            .OrderByDescending(x => x.startTime)
                            .ToListAsync();
                    }
                }

                // 将搜索结果显示到ListView
                foreach (var live in searchResults)
                {
                    var item = new ListViewItem(live.liveId);
                    item.SubItems.Add(live.uid.ToString());
                    item.SubItems.Add(live.name);
                    item.SubItems.Add(live.title);
                    item.SubItems.Add(DateTimeOffset.FromUnixTimeMilliseconds(live.startTime).ToString("yyyy-MM-dd HH:mm:ss"));
                    // 显示录像链接的状态，而不是完整链接（避免界面过于拥挤）
                    item.SubItems.Add(live.url);
                    item.SubItems.Add(live.url_backup);
                    item.Tag = live; // 存储完整对象供后续使用
                    listView_Data.Items.Add(item);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button_Search.Enabled = true;
                button_Search.Text = "查询";
            }
        }

        private async void button_GetUrl_Click(object sender, EventArgs e)
        {
            if (listView_Data.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择一条直播记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                button_GetUrl.Enabled = false;
                button_GetUrl.Text = "获取中...";

                var selectedItem = listView_Data.SelectedItems[0];
                var selectedLive = (AcfunLive)selectedItem.Tag;

                if (!string.IsNullOrEmpty(selectedLive.url))
                {
                    return;
                }

                // 调用API获取录播链接
                var form = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("liveId", selectedLive.liveId)
                };

                var requestUrl = "https://api.kuaishouzt.com/rest/zt/live/playBack/startPlay?subBiz=mainApp&kpn=ACFUN_APP&kpf=PC_WEB";
                var response = await _acfunApiRequest.Post(requestUrl, form);

                var result = JsonConvert.DeserializeObject<JToken>(response)?["result"]?.ToString();
                if (result is null || result != "1")
                {
                    MessageBox.Show("获取录播链接失败，API返回错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var responseObject = JsonConvert.DeserializeObject<GetPlaybackResponse>(response);
                var adaptiveManifest = JsonConvert.DeserializeObject<AdaptiveManifest>(responseObject.data.adaptiveManifest);

                // 获取录播链接并更新数据库
                if (adaptiveManifest.adaptationSet.Any() &&
                    adaptiveManifest.adaptationSet[0].representation.Any())
                {
                    // 获取主录播链接
                    var mainUrl = adaptiveManifest.adaptationSet[0].representation[0].url;
                    selectedLive.url = mainUrl;

                    // 获取备用录播链接（如果有）
                    if (adaptiveManifest.adaptationSet[0].representation[0].backupUrl.Any())
                    {
                        selectedLive.url_backup = adaptiveManifest.adaptationSet[0].representation[0].backupUrl[0];
                    }

                    // 保存到数据库
                    _dbContext.Update(selectedLive);
                    await _dbContext.SaveChangesAsync();

                    // 更新ListView显示
                    button_Search_Click(null, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("未找到有效的录播链接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取录播链接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button_GetUrl.Enabled = true;
                button_GetUrl.Text = "获取链接";
            }
        }
    }
}
