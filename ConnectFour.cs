using System;
using Gdk;
using Gtk;
using Cairo;
using Window = Gtk.Window;
using Color = Gdk.Color;


namespace ConnectFour{

	class Game{
		public const int columns = 7, rows = 6, n = 4;
		public enum FieldState : short {none = 0, red, yellow}; // type of this thing in the field

		private class GameState{
			/**
			* @board is the 2-dim array of FieldState. Here we save the information about the state of the game field
			* @currentPlayer is the  FieldState field. Here we contain information about the player who's turn now. We use only 'red' and 'blue' fields. We can use 'none' as an indicator for the end of the game (when somebody win.
			*/
			public FieldState[,] board;
			public FieldState currentPlayer;

			// logically this is a starting new game
			public GameState(){
				board = new FieldState[rows,columns];
				for(int y = 0; y < rows; y++)
					for(int x = 0; x < columns; x++)
						board[y,x] = FieldState.none;
				currentPlayer = FieldState.yellow;// let the blue player make turn first
			}
		}
		public class BoardCoordinates{
			public int row, column;
			public BoardCoordinates(int nr, int nc){
				row = nr;
				column = nc;
			}
		}

		private GameState state = new GameState();

		/**
		* @PutNextPiece - find the place where put a new piece with color 'player'
		*/
		private int PutNextPiece(int column, FieldState player){
			// if there's no empty fields in the column return -1
			if(state.board[0,column] != FieldState.none) return -1;

			//find the last empty row in the column and return it's number
			for(int row = 0; row < rows; row++){
				if(state.board[row, column] != FieldState.none){
					state.board[row-1, column] = player;
					return row-1;
				}
			}
			// if all rows are empty, put the piece in the last row
			state.board[rows-1, column] = player;
			return rows - 1;
		}

		public FieldState this[int row,int col]{
			get => state.board[row,col];
		}
		public FieldState this[BoardCoordinates coor]{
			get => state.board[coor.row, coor.column];
		}
		public FieldState player{
			get => state.currentPlayer;
		}


		/**
		* in the next functions is all the logic of the game
		* @NextTurn(int column) - make a next turn in the game and use an event for viewer-controller for do what it must do
		* @WinCombination(int column, int row) - return an array of coordinates where is a win-combination
		* @IsStaleMate() - all fields are not empty
		* @NewGame() - is a method for starting new game
		*/

		public delegate void Notify();
		public event Notify changed;

		public void NextTurn(int column){
			// check if it's end of game
			if(state.currentPlayer == FieldState.none){
				return;
			}

			// set a new piece in the column and put a number of row to the event
			BoardCoordinates coor = new BoardCoordinates(PutNextPiece(column, state.currentPlayer), column);
			if(coor.row != -1){
				changed();

				// if currentPlayer won end the game
				if(WinCombination()){
					state.currentPlayer = FieldState.none;
					return;
				}

				// toggle player
				state.currentPlayer = (state.currentPlayer == FieldState.red) ? FieldState.yellow : FieldState.red;
			}
			// we can get a stalemate iff when for all column PutNextPiece return -1
			// if it's now stalemate end the game
			else if(IsStalemate()){
				state.currentPlayer = FieldState.none;
			}
		}

		public void NewGame(){
			state = new GameState();
			winCombination = null;
		}

		// Win Situation
		public BoardCoordinates[] winCombination = null;



		public bool checkIfPositionIsAvaliableOnTheBoard(BoardCoordinates coor){
			return coor.row >=0 && coor.row < rows && coor.column >= 0 && coor.column < columns;
		}
		public bool checkFieldInDirection(BoardCoordinates coor, int dRow, int dCol){
			/**
			* check field in direction dCoor from coor
			* if the combination is winning, write it into the field winCombination
			*/
			BoardCoordinates[] combination = new BoardCoordinates[n];
			FieldState winner = this[coor];
			if(winner == FieldState.none)
				return false;
			for(int i = 0; i < n; i++){
				BoardCoordinates next = new BoardCoordinates(coor.row + i * dRow, coor.column + i * dCol);
				if(checkIfPositionIsAvaliableOnTheBoard(next) && winner == this[next]){
					combination[i] = next;
				}
				else return false;
			}
			winCombination = combination;
			return true;

		}
		public bool WinCombination(){
			/**
			* check all directions for find winning combination
			* if we found return true
			*/
			for(int row = 0; row < rows; row++){
				for(int col = 0; col < columns; col++){
					BoardCoordinates coor = new BoardCoordinates(row, col);
					if(checkFieldInDirection(coor, 0, 1) /*row*/ || checkFieldInDirection(coor, -1, 0)/*column*/ ||
						checkFieldInDirection(coor, -1, 1)/*up-right*/ || checkFieldInDirection(coor, -1, -1)/*up-left*/)
						return true;
				}
			}
			return false;

		}
		public bool IsStalemate(){
			for(int column = 0; column < columns; column++){
				if(state.board[0, column] == FieldState.none) return false;
			}
			return true;
		}
	}




	class View : DrawingArea{
		/**
		 * when button pressed - color it red
		 * when release - color white
		 */
		int BoardWidth = 700, BoardHeight = 600,
			   fieldSize = 100, holeRadius = 45, fieldMargin = 5, currentColumn = 0;

		Game game = new Game();


		public View(/*int width, int height*/) : base() {

			AddEvents((int) EventMask.ButtonPressMask +
					  (int) EventMask.PointerMotionMask);
			game.changed += QueueDraw;
		}

		protected override bool OnExposeEvent (EventExpose ev) {
			Context c = CairoHelper.Create(GdkWindow);

			c.SetSourceRGB((double) 111/(double) 255,(double)  120/ (double) 255,(double)  237/(double) 255);
			c.Rectangle(0,0,BoardWidth, BoardHeight);
			c.Fill();


			// ToDo: draw win positions
			if(game.winCombination != null){
				for(int i = 0; i < game.winCombination.Length; i++){
					int row = game.winCombination[i].row, column = game.winCombination[i].column;
					// green color
					c.SetSourceRGB((double) 24/(double) 255,(double)  227/(double) 255, (double) 111/(double) 255);
					c.Rectangle(getX(column), getY(row), fieldSize, fieldSize);
					c.Fill();
				}
			}

			// draw pieces in the holes

			for(int row = 0; row < Game.rows; row++){
				for(int col = 0; col < Game.columns; col++){
					// set color of field
					if(game[row,col] == Game.FieldState.yellow){
						// yellow
						c.SetSourceRGB(1.0, 1.0, (double) 74/(double) 255);
					}
					else if(game[row,col] == Game.FieldState.red){
						// red
						c.SetSourceRGB((double) 250/(double) 255, (double) 40/(double) 255, (double) 40/(double) 255);
					}
					else c.SetSourceRGB((double) 223/(double) 255, (double) 215/(double) 255, (double) 215/(double) 255);

					c.Arc(getX(col) + fieldMargin + holeRadius, getY(row) + fieldMargin + holeRadius, holeRadius, 0, 2*Math.PI);
					c.Fill();
				}
			}


			// draw piece in the 0. row that will fall down
			if(game[0, currentColumn] == Game.FieldState.none){
				if(game.player == Game.FieldState.yellow){
					// yellow
					c.SetSourceRGB(1.0, 1.0, (double) 74/(double) 255);
				}
				else if(game.player == Game.FieldState.red){
					// red
					c.SetSourceRGB((double) 250/(double) 255, (double) 40/(double) 255, (double) 40/(double) 255);
				}
				c.Arc(getX(currentColumn) + fieldMargin + holeRadius, 0 + fieldMargin + holeRadius, holeRadius, 0, 2*Math.PI);
				c.Fill();
			}

			c.GetTarget().Dispose();
			c.Dispose();
			return true;
		}



		protected override bool OnMotionNotifyEvent (EventMotion ev){
			if(game.player != Game.FieldState.none){
				currentColumn = (int) ev.X / fieldSize;
				QueueDraw();
			}
			return true;
		}


		protected override bool OnButtonPressEvent (EventButton ev){
			// check if is end of game
			if(game.player == Game.FieldState.none){
				game.NewGame();
			}
			else{
				game.NextTurn(getColumn(ev.X));			}

			return true;
		}


		// return column of the cursor
		private int getColumn(double x){
			return ((int) x / fieldSize);
		}

		// return X-coordinate of the left upper corner of the field in column col
		private int getX(int col){
			return (col * fieldSize);
		}

		// Y-coord
		private int getY(int row){
			return (row * fieldSize);
		}


	}


	class Frame : Window {
		Frame() : base("Connext four") {
			Add(new View());
			SetDefaultSize(700, 600);

		}

		protected override bool OnDeleteEvent(Event ev) {
			Application.Quit();
			return true;
		}

		static void Main() {


			Application.Init();
			new Frame().ShowAll();
			Application.Run();

		}


	}



}
