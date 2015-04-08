using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Connect {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

			double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
			double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
			double windowWidth = this.Width;
			double windowHeight = this.Height;
			this.Left = screenWidth - windowWidth - 50;
			this.Top = (screenHeight / 2) - (windowHeight / 2);

			if (!Directory.Exists(ffFolder)) {
				Directory.CreateDirectory(ffFolder);
			}

			if (!File.Exists(ffSave)) {
				using (StreamWriter sw = new StreamWriter(ffSave)) {
					sw.Write("0");
				}
			}

			using (StreamReader sr = new StreamReader(ffSave)) {
				HighScore = Convert.ToInt32(sr.ReadLine());
			}
		}

		string ffFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Connect";
		string ffSave = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Connect\score.txt";

		double TotalTime = 60;
		int NowScore, HighScore;
		DispatcherTimer MainTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10), };
		DispatcherTimer ReadyTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1500), };

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			MainTimer.Tick += MainTimer_Tick;
			ReadyTimer.Tick += ReadyTimer_Tick;

			ResetGame(false);
			//FillBoard();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			SaveHighScore();
		}

		private void SaveHighScore() {
			using (StreamWriter sw = new StreamWriter(ffSave)) {
				sw.WriteLine(HighScore.ToString());
			}
		}

		private void ButtonStart_Click(object sender, RoutedEventArgs e) {
			textStartArrow.Visibility = Visibility.Collapsed;
			((sender as Button).Content as TextBlock).Text = "RESTART";
			ResetGame();
		}

		// Init game

		private void ResetGame(bool isStart = true) {
			Array.Clear(Board, 0, Board.Length);
			gridBoard.Children.Clear();
			DictNode.Clear();

			NowScore = 0;
			RefreshScore();

			if (isStart) {
				isClickable = false;

				gridBoard.IsHitTestVisible = true;
				gridResult.Visibility = Visibility.Collapsed;

				MainTimer.Stop();
				textTimer.Text = string.Format("{0}:00", TotalTime);

				ReadyCount = 0;
				textReady.Visibility = Visibility.Visible;
				textGo.Visibility = Visibility.Collapsed;
				ReadyTimer.Stop();
				ReadyTimer.Start();
			}
		}

		int ReadyCount;
		private void ReadyTimer_Tick(object sender, EventArgs e) {
			switch (++ReadyCount) {
				case 1:
					textReady.Visibility = Visibility.Collapsed;
					textGo.Visibility = Visibility.Visible;
					break;
				case 2:
					textGo.Visibility = Visibility.Collapsed;
					EndTime = DateTime.Now + TimeSpan.FromSeconds(TotalTime);
					MainTimer.Start();
					FillBoard();
					break;
				case 3:
					(sender as DispatcherTimer).Stop();
					break;
				default:
					break;
			}
		}

		DateTime EndTime;
		void MainTimer_Tick(object sender, EventArgs e) {
			TimeSpan tGap = EndTime - DateTime.Now;

			if (tGap <= TimeSpan.FromSeconds(0)) {
				textTimer.Text = "0.00";
				(sender as DispatcherTimer).Stop();
				ShowResult();
			} else {
				textTimer.Text = string.Format("{0}.{1:D2}", tGap.Seconds, tGap.Milliseconds / 10);
			}
		}

		private void RefreshScore() {
			if (HighScore < NowScore) { HighScore = NowScore; }
			textScore.Text = string.Format("You : {0} / Highscore : {1}", NowScore, HighScore);
		}

		// Game process

		int[,] Board = new int[8, 8];
		int NodeID = 1;
		Dictionary<int, Node> DictNode = new Dictionary<int, Node>();
		double EllipseRadius = 25;
		Random rand = new Random();

		SolidColorBrush[] ColorArray = new SolidColorBrush[] { 
			Brushes.Red, Brushes.Blue, Brushes.Purple, Brushes.Orange, Brushes.Black };

		int[] dx = new int[4] { 1, 0, -1, 0 };
		int[] dy = new int[4] { 0, 1, 0, -1 };
		private void RemoveNode(int x, int y, int val) {
			Queue<int> q = new Queue<int>();
			List<int> listRemove = new List<int>();
			bool[,] check = new bool[8, 8];

			q.Enqueue(x); q.Enqueue(y);

			do {
				x = q.Dequeue();
				y = q.Dequeue();

				for (int i = 0; i < 4; i++) {
					if (y + dy[i] < 0 || x + dx[i] < 0 || y + dy[i] >= 8 || x + dx[i] >= 8) { continue; }
					if (!check[y + dy[i], x + dx[i]]
						&& DictNode[Board[y + dy[i], x + dx[i]]].Value == val) {

						listRemove.Add(Board[y + dy[i], x + dx[i]]);

						check[y + dy[i], x + dx[i]] = true;
						//Board[y + dy[i], x + dx[i]] = 0;

						q.Enqueue(x + dx[i]);
						q.Enqueue(y + dy[i]);
					}
				}
			} while (q.Count > 0);

			if (listRemove.Count == 0) {
				isClickable = true;
				return;
			}

			Storyboard sb = new Storyboard();
			foreach (int id in listRemove) {
				Board[DictNode[id].Y, DictNode[id].X] = 0;

				sb.Children.Add(GetScaleAnimation(0, DictNode[id].GridBase));
				sb.Children.Add(GetScaleAnimation(1, DictNode[id].GridBase));

				DictNode.Remove(id);
			}

			NowScore += listRemove.Count * listRemove.Count;
			RefreshScore();

			sb.Completed += sbRemove_Completed;
			sb.Begin(this);
		}

		private void sbRemove_Completed(object sender, EventArgs e) {
			FillBoard();
		}

		private void FillBoard() {
			Storyboard sb = new Storyboard();
			int[] lineblank = new int[8];

			string log = "";
			for (int i = 0; i < 8; i++) {
				for (int j = 0; j < 8; j++) {
					log += string.Format("{0:D2} ", Board[i, j]);
				}
				log += "\n";
			}

			for (int j = 0; j < 8; j++) {
				lineblank[j] = 0;
				for (int i = 7; i >= 0; i--) {
					if (Board[i, j] == 0) {
						lineblank[j]++;
						for (int k = i - 1; k >= 0; k--) {
							if (Board[k, j] > 0) {
								Board[i, j] = Board[k, j];
								DictNode[Board[i, j]].Y = i;

								sb.Children.Add(GetThicknessAnimation(
									500,
									GetPosition(DictNode[Board[k, j]].X, DictNode[Board[k, j]].Y).X,
									GetPosition(DictNode[Board[k, j]].X, DictNode[Board[k, j]].Y).Y,
									DictNode[Board[k, j]].GridBase));

								Board[k, j] = 0;
								lineblank[j]--;
								break;
							}
						}
					}
				}
			}

			log = "";
			for (int i = 0; i < 8; i++) {
				log += string.Format("{0} ", lineblank[i]);
			}

			int val;
			for (int j = 0; j < 8; j++) {
				for (int i = 0; i < lineblank[j]; i++) {
					val = rand.Next(5);

					Node node = new Node() {
						ID = NodeID, X = j, Y = i,
						Type = 1, Value = val,
						GridBase = new Grid() {
							Tag = NodeID,
							Background = Brushes.Transparent,
							Width = 45,
							Height = 45,
							Margin = new Thickness(j * 45, -45, 0, 0),
							Opacity = 0.1,
						}
					};

					Ellipse el = new Ellipse() {
						Fill = ColorArray[val],
						Width = EllipseRadius,
						Height = EllipseRadius,
					};

					node.GridBase.Children.Add(el);

					node.GridBase.MouseEnter += EllipseBase_MouseEnter;
					node.GridBase.MouseLeave += EllipseBase_MouseLeave;
					node.GridBase.MouseDown += EllipseBase_MouseDown;

					gridBoard.Children.Add(node.GridBase);
					sb.Children.Add(GetThicknessAnimation(500, GetPosition(node.X, node.Y).X, GetPosition(node.X, node.Y).Y, node.GridBase, 0, 0, 300));
					sb.Children.Add(GetDoubleAnimation(0.5, node.GridBase, 400, 300));

					Board[i, j] = node.ID;
					DictNode.Add(node.ID, node);

					NodeID++;
				}
			}

			isClickable = true;

			sb.Begin(this);
		}

		bool isClickable = true;
		private void EllipseBase_MouseDown(object sender, MouseButtonEventArgs e) {
			if (!isClickable) { return; }
			isClickable = false;

			int id = (int)(sender as Grid).Tag;
			if (!DictNode.ContainsKey(id)) {
				return;
			}
			RemoveNode(DictNode[id].X, DictNode[id].Y, DictNode[id].Value);
		}

		private void EllipseBase_MouseEnter(object sender, MouseEventArgs e) {
			(sender as Grid).BeginAnimation(Grid.OpacityProperty, null);
			(sender as Grid).Opacity = 1;
		}
		private void EllipseBase_MouseLeave(object sender, MouseEventArgs e) {
			(sender as Grid).BeginAnimation(Grid.OpacityProperty, null);
			(sender as Grid).Opacity = 0.5;
		}

		private IntPair GetPosition(int X, int Y) {
			IntPair ip = new IntPair();
			//ip.X = X * 45 + 22.5 - EllipseRadius / 2;
			//ip.Y = Y * 45 + 22.5 - EllipseRadius / 2;
			ip.X = X * 45;
			ip.Y = Y * 45;

			return ip;
		}

		private void ShowResult() {
			gridResult.Visibility = Visibility.Visible;
			gridBoard.IsHitTestVisible = false;

			textResultScore.Text = string.Format("SCORE {0}", NowScore);
			textResultHighscore.Text = string.Format("HIGHSCORE {0}", HighScore);

			if (NowScore == HighScore) {
				textComment.Text = "Congratulations!";
			} else {
				textComment.Text = "Try again";
			} 
			
			SaveHighScore();
		}

		private DoubleAnimation GetScaleAnimation(int xy, FrameworkElement fe = null, double duration = 400) {
			DoubleAnimation da = new DoubleAnimation(0, TimeSpan.FromMilliseconds(duration)) {
				EasingFunction = new BackEase() {
					//Exponent = 5, 
					Amplitude = 0.8,
					EasingMode = EasingMode.EaseIn,
				},
			};
			Storyboard.SetTarget(da, fe);

			if (xy == 0) {
				fe.RenderTransformOrigin = new Point(0.5, 0.5);
				fe.RenderTransform = new ScaleTransform(1, 1);

				Storyboard.SetTargetProperty(da, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
			} else {
				Storyboard.SetTargetProperty(da, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
			}

			return da;
		}
		private ThicknessAnimation GetThicknessAnimation(double duration, double left, double top, FrameworkElement fe = null, double right = 0, double bottom = 0, double delay = 0) {
			ThicknessAnimation ta = new ThicknessAnimation(
					new Thickness(left, top, right, bottom),
					TimeSpan.FromMilliseconds(duration)) {
						//EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut, },
						EasingFunction = new BackEase() { Amplitude = 0.4, EasingMode = EasingMode.EaseOut },
						BeginTime = TimeSpan.FromMilliseconds(delay)
					};

			if (fe != null) {
				Storyboard.SetTarget(ta, fe);
				Storyboard.SetTargetProperty(ta, new PropertyPath(FrameworkElement.MarginProperty));
			}

			return ta;
		}
		private DoubleAnimation GetDoubleAnimation(double opacity, FrameworkElement fe, double duration = 300, double delay = 0) {
			DoubleAnimation da = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(duration)) {
				BeginTime = TimeSpan.FromMilliseconds(delay),
			};
			Storyboard.SetTarget(da, fe);
			Storyboard.SetTargetProperty(da, new PropertyPath(FrameworkElement.OpacityProperty));

			return da;
		}
	}

	public class Node {
		public int ID, X, Y, Value, Type;
		public Grid GridBase;
	}

	public class IntPair {
		public double X, Y;
		public static IntPair MakePair(double f, double s) {
			return new IntPair() { X = f, Y = s };
		}
	}
}
