   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5      %    Avtoservis.Admin_Rabochie_mesta   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      *localclr-namespace:Avtoservis     5      -   5          N� Title$  Работы��5	       N� FontSize$ 16(�6    N� 
FontFamily$ Century Gothic)�6$    � UseLayoutRounding .  J� SnapsToDevicePixels .  � Cursor$  ��6�   .��5
      � .+�5   
   �� 
������� �� CornerRadius$ 15��5      $��50q�5       �� Padding$ 30q�5      ._�5      �  � RowDefinitions 5      �� 5      $
��Auto �6$   �� 5      $
��Auto �6$   �� 5      $��* �6$   �� 5      $
��Auto �6$   	 � ColumnDefinitions	 5      �� 5      $	��400 �6'   �� 5      $��* �6'   .+�5      �� $��Рабочие места��6   $��2��5      $��24(�5      $
��Bold"�5       $��	40,0,0,30q�5!      �� 5$      $��1��6   
 �� Orientation$
 
Horizontal=�6*   $
��Left=�6C   .+�5%      �� -   5         ��Name$ BtnAdd��5%      $��Добавить��6-      AddButtonStyle#��[  6@   $��	40,0,10,0q�6h   6{   �� 5&      -   5        $ 	BtnDelete��5&      $��Удалить��60     DeleteButtonStyle#��[ 6B   6m   �� 5'      -   5        $ 
BtnRefresh��5'        RoundedButtonStyle#��[ 61   $��35��6]   $��35��6h   $
 Hand��6�   ���	#FFA0A0A0
����������	#FF042271.��5(      �� $��/Images/refresh.png��6    $��Uniform=�6=   $��1,0,0,0q�6O   $��19��6`   $��19��6k   5)      5*      �� 5-      -   5         ��Name$ SearchTextBox��5-      $��1��61   $��1��6>     SearchTextBoxStyle#��[ 6N   $��Поиск...��6z   $��Right=�6�   6�   �� 50      -   5         ��Name$ Rabochie_mesta_List��50      $��2��68   $��40,20,40,20q�6E   $��2��6Z   $��Stretch=�6n   $��Disabled=�51        WorksListViewStyle#��[ 52       �� SelectionMode$ Single=�53       �� AlternationCount$
 1000��54       �� View 56      �� 57      .P�59      �� $��ID��6-    �� HeaderContainerStyle 5:   "   �� 5;   &    �� 
TargetType" ��6,     SortableHeaderStyle �� BasedOn# [ 6N   .�5<   *   -   5        5=   '    �� CellTemplate 5?   "   �� 5@   &   .R�5A   *   �� $��Center=�5B   4   
���   �$��Center=�5C   4   ��5A   4   �� *ID_rabochego_mesta+5C   4   5D   '   5F      �� 5I      $��Иконка��6-    5J   "   �� 5K   &   .R�5L   *   �� $��40��5M       $��40��6+   $��Uniform=�67   $��5q�6I   $��Center=�5N       $��Center=�6=   ��5L   0   �� *	IconImage+5N   =   5O   '   5Q      �� 5T      $��Наименование��6-    5U   "   �� 5V   &   .R�5W   *   �� $��Center=�5X   4   
���   �$��Center=�5Y   4   ��5W   4   �� *Rabochee_mesto+5Y   4   5Z   '   5\      �� 5_       5`   "   �� 5a   &   .R�5b   *   �� -   5        $
 Hand��5b   G   $��40��6U   $��40��6`   $��Center=�6l   
������ 
������ .��5c   .   �� $��/Images/icon_edit.png��64   $��Uniform=�6S   $��35��6e   $��35��6p   5d   +   5e   '   5g      5h      5j      �� 5m      $��3��6   $��2��6*   $
 
Horizontal=�6>   $��Right=�6W   $��
0,10,40,10q�6s   .+�5o      �� $��Показывать:��6    $��Center=�63   $��0,0,10,0q�6N   $��14(�6`   �� 5p      -	   5        $ ItemsPerPageButton��5p      $��20��69     ComboBoxStyle#��[ 5q      5r      �� 5s      -
   5         ��Name$ PaginationInfo��5s       $��1-20 из 100��68   $��Center=�6K   $��	20,0,20,0q�6f   $��14(�6y   �� 5u      -   5        $ BtnPrevPage��5u      $��
Назад��62    ! PaginationButtonDisabledStyle#��[ 6B   $��0,0,10,0q�6y   6�   �� 5v      -   5        $ BtnNextPage��5v      $��Вперед��62   #��[ 6C   $��0,0,0,0q�6z   6�   5w      5y      5z      5{      5|      