using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Data.SqlClient; 
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.Configuration;

namespace DateB
{
	public partial class Form1 : Form
	{
		//создание соединения
		SqlConnection sqlConnection;

		//создание листа, который будет заполнен как бд
		List<Product> products = new List<Product>();

		//лист с исключениями для фильтрации
		List<int> exceptionId = new List<int>();

		//!async - запускает доп Task, чтоб интерфейс не тормозил, поэтому все методы ...Async(), await не забываем
		public Form1()
		{
			InitializeComponent();
		}

		//FORM CUSTOMIZATION / CREATE CONNECTION
		private async void Form1_Load(object sender, EventArgs e)
		{
			//ссылка на базу. настройка соединения
			//using Syst.Confi.., заходим в App.config, и там химичим
			string connectionString = ConfigurationManager.ConnectionStrings["StringBD"].ConnectionString;
			sqlConnection = new SqlConnection(connectionString);

			//открываем соединение
			await sqlConnection.OpenAsync();

			//вывод таблицы
			updateToolStripMenuItem_Click(sender, e);
		}

		//LOAD TABEL
		private async void updateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//tabControl1.SelectedIndex = 0; переход между вкладками (0, 1, 2...)
			//очищаем данные до этого, а то каша получится
			listBox1.Items.Clear();

			//читаем данные
			SqlDataReader sqlReader = null;

			//команда на чтение . [взять все из базы]
			SqlCommand command = new SqlCommand("SELECT * FROM [Products]", sqlConnection);
			try
			{
				//выполняем команду
				sqlReader = await command.ExecuteReaderAsync();
				listBox1.Items.Add("ID    Название:              Объём:    Цена:");
				//проход по всей базе
				while (await sqlReader.ReadAsync())
				{
					//магия с выравниванием колонок, и не спрашивай как, сам не знаю))
					string space1 = "      ";
					string space2 = "";
					int countS2 = 16 - Convert.ToString(sqlReader["Name"]).Length;
					for (int i = 0; i < countS2; i++)
						space2 += "  ";
					string space3 = "      ";
					if (Convert.ToInt32(sqlReader["Id"]) >= 10)
						space1 = "    ";

					//переменные для облегчения
					string id = Convert.ToString(sqlReader["Id"]);
					string name = Convert.ToString(sqlReader["Name"]);
					string vol = Convert.ToString(sqlReader["Volume"]);
					string pr = Convert.ToString(sqlReader["Price"]);

					//заполнение листа, для долнейшей фильтрации
					products.Add(new Product(int.Parse(id), name, int.Parse(vol), int.Parse(pr)));

					//вывод ее пользователю в листБокс1
					if (!exceptionId.Contains(int.Parse(id)))
					{
						listBox1.Items.Add(id + space1 + name + space2 + vol + " мл" + space3 + pr + "р");
					}
				}
			}
			//вдруг ошибка(
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message.ToString(), ex.Source.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			//под конец можно и чтение закрыть
			finally
			{
				if (sqlReader != null)
					sqlReader.Close();
			}
		}

        #region FILTER
        //ФИЛЬТРАЦИЯ
        private void filter_Click(object sender, EventArgs e)
		{
			//очистка исключений
			exceptionId.Clear();

			//заранее чистим предупреждения
			clearLabel(label13);

			int minPrice = products.Min(p => p.Price);
			int maxPrice = products.Max(p => p.Price);
			int minVolume = products.Min(p => p.Volume);
			int maxVolume = products.Max(p => p.Volume);
			bool exist = false;

			//проверка на заполненость полей
			if (!string.IsNullOrEmpty(textBox9.Text) && !string.IsNullOrWhiteSpace(textBox9.Text))
			{
				minPrice = int.Parse(textBox9.Text);
				exist = true;
			}
			if (!string.IsNullOrEmpty(textBox10.Text) && !string.IsNullOrWhiteSpace(textBox10.Text))
			{
				maxPrice = int.Parse(textBox10.Text);
				exist = true;
			}
			if (!string.IsNullOrEmpty(textBox11.Text) && !string.IsNullOrWhiteSpace(textBox11.Text))
			{
				minVolume = int.Parse(textBox11.Text);
				exist = true;
			}
			if (!string.IsNullOrEmpty(textBox12.Text) && !string.IsNullOrWhiteSpace(textBox12.Text))
			{
				maxVolume = int.Parse(textBox12.Text);
				exist = true;
			}

			if (!exist)
			{
				//вывод провального сообщения
				failedMessage(label13, "Хоть что-то заполни)");

				//перезагрузка таблицы
				updateToolStripMenuItem_Click(sender, e);
				return;
			}
			
			//поиск икслючений
			var find = from p in products
					   where !(p.Price <= maxPrice && p.Price >= minPrice) ||
					   !(p.Volume <= maxVolume && p.Volume >= minVolume)
					   select p;

			//добавление исключений в лист
			foreach (Product p in find)
				exceptionId.Add(p.Id);

			//вывод успешного сообщения
			successfulMessage(label13, "Отполированно!", new List<TextBox> { });

			//перезагрузка таблицы
			updateToolStripMenuItem_Click(sender, e);
		}

		//сброс фильтров
		private void discharge_Click(object sender, EventArgs e)
		{
			//очистка исключений
			exceptionId.Clear();

			//заранее чистим предупреждения
			clearLabel(label13);

			if (!string.IsNullOrEmpty(textBox9.Text) && !string.IsNullOrWhiteSpace(textBox9.Text) ||
				!string.IsNullOrEmpty(textBox10.Text) && !string.IsNullOrWhiteSpace(textBox10.Text) ||
				!string.IsNullOrEmpty(textBox11.Text) && !string.IsNullOrWhiteSpace(textBox11.Text) ||
				!string.IsNullOrEmpty(textBox12.Text) && !string.IsNullOrWhiteSpace(textBox12.Text))
			{
				//вывод успешного сообщения
				successfulMessage(label13, "Сброшенно", new List<TextBox> { textBox9, textBox10, textBox11, textBox12 });

				//перезагрузка таблицы
				updateToolStripMenuItem_Click(sender, e);
			}
			else
			{
				//вывод провального сообщения
				failedMessage(label13, "Что ты хочешь чтоб я стер, а?");
			}
		}
        #endregion

        #region INSERT
        private async void insert_Click(object sender, EventArgs e)
		{
			//заранее чистим предупреждения
			clearLabel(label7);

			//проверка на заполненость полей
			if (!string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrWhiteSpace(textBox1.Text) &&
				!string.IsNullOrEmpty(textBox2.Text) && !string.IsNullOrWhiteSpace(textBox2.Text) &&
				!string.IsNullOrEmpty(textBox7.Text) && !string.IsNullOrWhiteSpace(textBox7.Text))
			{
				//задание команды
				SqlCommand command = new SqlCommand("INSERT INTO [Products] (Name, Volume, Price)VALUES(@Name, @Volume, @Price)", sqlConnection);

				//берем переменные из текстБоксов
				command.Parameters.AddWithValue("Name", textBox1.Text);
				command.Parameters.AddWithValue("Price", textBox2.Text);
				command.Parameters.AddWithValue("Volume", textBox7.Text);

				//выполнение команды(она ничего не вернет)
				await command.ExecuteNonQueryAsync();

				//вывод успешного сообщения
				successfulMessage(label7, "Смастренно!", new List<TextBox> { textBox1, textBox2, textBox7});

				//перезагрузка таблицы
				updateToolStripMenuItem_Click(sender, e);
			}
			else 
			{
				//вывод провального сообщения
				failedMessage(label7, "Поля заполни, клоун!");
			}
		}
		#endregion

		#region UPDATE
		private async void update_Click(object sender, EventArgs e)
		{
			//заранее чистим предупреждения
			clearLabel(label8);

			//проверка на заполненость полей
			if (!string.IsNullOrEmpty(textBox3.Text) &&!string.IsNullOrWhiteSpace(textBox3.Text) &&
				!string.IsNullOrEmpty(textBox6.Text) && !string.IsNullOrWhiteSpace(textBox6.Text) &&
				!string.IsNullOrEmpty(textBox4.Text) && !string.IsNullOrWhiteSpace(textBox4.Text) &&
				!string.IsNullOrEmpty(textBox8.Text) && !string.IsNullOrWhiteSpace(textBox8.Text))
			{
				//задание команды
				SqlCommand command = new SqlCommand("UPDATE [Products] SET [Name]=@Name, [Volume]=@Volume, [Price]=@Price WHERE [Id]=@Id", sqlConnection);

				//берем переменные из текстБоксов
				command.Parameters.AddWithValue("Name", textBox6.Text);
				command.Parameters.AddWithValue("Volume", textBox8.Text);
				command.Parameters.AddWithValue("Id", textBox3.Text);
				command.Parameters.AddWithValue("Price", textBox4.Text);

				//выполнение команды(она ничего не вернет)
				await command.ExecuteNonQueryAsync();

				//вывод успешного сообщения
				successfulMessage(label8, "Заменяно!", new List<TextBox> { textBox3, textBox4, textBox6, textBox8 });

				//перезагрузка таблицы
				updateToolStripMenuItem_Click(sender, e);
			}
			else
			{
				//вывод провального сообщения
				failedMessage(label8, "Поляяяя! сколько можно, заполни их");
			}
		}
		#endregion

		#region DELETE
		private async void delete_Click(object sender, EventArgs e)
		{
			//заранее чистим предупреждения
			clearLabel(label9);

			//проверка на заполненость полей
			if (!string.IsNullOrEmpty(textBox5.Text) && !string.IsNullOrWhiteSpace(textBox5.Text))
			{
				//задание команды
				SqlCommand command = new SqlCommand("DElETE FROM [Products] WHERE [Id]=@Id", sqlConnection);

				//берем переменную из текстБокса
				command.Parameters.AddWithValue("Id", textBox5.Text);

				//выполнение команды(она ничего не вернет)
				await command.ExecuteNonQueryAsync();

				//вывод успешного сообщения
				successfulMessage(label9, "Подтерто!", new List<TextBox> { textBox5 });

				//перезагрузка таблицы
				updateToolStripMenuItem_Click(sender, e);
			}
			else
			{
				//вывод провального сообщения
				failedMessage(label9, "Где Id, ты хочешь чтоб я всю базу стер?");
			}
		}
        #endregion

        //выбрать выделенный
        private void selectDedicated_Click(object sender, EventArgs e)
		{
			//заранее чистим предупреждения
			clearLabel(label9);
			textBox5.Clear();

			//id выделенного айтем
			int selectedIdOfItem = listBox1.SelectedIndex;

			//а вдруг не выделенно
			if (selectedIdOfItem == -1)
			{
				//вывод провального сообщения
				failedMessage(label9, "Выдели че-нибудь, плз!");
			}
			else
			{
				//получаем весь айтем
				string selectedItem = listBox1.Items[selectedIdOfItem].ToString();
				//только его id
				var selectedIdOfProduct = selectedItem.Split(' ').Select(x => x).ToArray()[0];
				try
				{
					//это хоть число?
					int i = int.Parse(selectedIdOfProduct);
					textBox5.Text = selectedIdOfProduct.ToString();
				}
				catch
				{
					//вывод провального сообщения
					failedMessage(label9, "не-не, не шали!");
				}
				finally { }
			}
		}

		//чистим предупреждения
		private void clearLabel(Label l)
		{
			if (l.Visible)
			{
				l.Visible = false;
				l.ForeColor = Color.Red;
			}
		}

		//формирование успешных сообщений и стрерание заполненных полей
		private void successfulMessage(Label l, string text, List<TextBox> list)
		{
			foreach (TextBox t in list)
				t.Clear();
			l.Visible = true;
			l.ForeColor = Color.Green;
			l.Text = text;
		}

		//формирование провальных сообщений
		private void failedMessage(Label l, string text)
		{
			l.Visible = true;
			l.Text = text;
		}

        #region CLOSE CONNECTION
        //закрытие соединения, чтобы не было утечки данных - по кнопке
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
				sqlConnection.Close();
			Close();
		}

		//закрытие соединения, чтобы не было утечки данных - по крестику
		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
				sqlConnection.Close();
		}
        #endregion
    }

    //класс копирующий базу данных
    public class Product
	{
		public int Id { get; set; }
		public string ProductName { get; set; }
		public int Volume { get; set; }
		public int Price { get; set; }

		public Product(int id, string name, int vol, int pr)
		{
			Id = id;
			ProductName = name;
			Volume = vol;
			Price = pr;
		}
	}

	public class Tesst
	{
		int Ou { get; set; }
		private string Name;
		public Tesst(int test, string n)
		{
			Ou = test;
			Name = n;
		}
	}
}