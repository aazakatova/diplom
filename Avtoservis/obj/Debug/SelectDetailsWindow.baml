   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5   	   $    Avtoservis.SelectDetailsWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:Avtoservis     5   	   -   5          � Title$  Выбор деталей��5   	   $	��640��6   $
��1050��6,   $	��640��69   $
��1050��6I    � WindowStartupLocation$ CenterScreen=�5	   	   $��Century Gothic)�5
   	   $��16(�6%   .��5      � ���	#FF282B38.+�5   
   �� 
������� �� CornerRadius$ 15��6$   $��20q�66   ._�5      �  � RowDefinitions 5      �� 5      $
��auto �6$   �� 5      $
��auto �6$   �� 5      $
��360* �6$   �� 5      $	��75* �6$   .+�5      �� $��Детали��6   $��24(�6*   $
��Bold"�68   $��Center=�6J   $
��Left=�6e   $��30,20,20,10q�5      $��0��61   ���	#FF567BFF6>   �� 5      -   5         ��Name$ SearchTextBox��5      $��1��61      SearchTextBoxStyle#��[  6>   $��Поиск...��5      $��20,10,20,10q�6*   $	��300��6?   $
��Left=�6K   $��Center=�6f   5   T   �� 5      -   5         ��Name$ DetailsList��5      $��2��60   $��
20,10,20,0q�6=     WorksListViewStyle#��[ 5       $��Stretch=�5!       �� SelectionMode$ Single=�5"      $��Disabled=�5#      $	��350��5$       �� View 5&      �� 5'      .P�5(      �� $��Выбрать��6-    �� Width$
 Auto��6>   	 �� CellTemplate	 5)   "   �� 5*   &   .R�5+   *   �� $��Center=�6T   $��Center=�5,   #   
 d� 	IsChecked
 5+   3   �� *
IsSelected+5,   #   5-   '   5/      �� 51      $��Деталь��6-    �� HeaderContainerStyle 52   "   �� 53   &    �� 
TargetType" ��6,     SortableHeaderStyle �� BasedOn# [ 6N   .�54   *   -   5        55   '   	 57   "   �� 58   &   .R�59   *   �� $��Center=�5:   $   $��Center=�5;   $   $
��Wrap=�5<   $    �� TextAlignment$ Center=�5=   $   $
��10,0q�5>   $   ��59   4   �� *Detail.Nazvanie+5>   $   5?   '   5A      �� 5C      $��Тип��6-    5D   "   �� 5E   &   " ��6,   # [ 6N   .�5F   *   -   5        5G   '   	 5I   "   �� 5J   &   .R�5K   *   �� $��Center=�5L   $   $��Center=�5M   $   ��5K   4   �� *&$Detail.dm_Tipi_detalei.Nazvanie_tipa+5M   $   5N   '   5P      �� 5R      $��Цена��6-    5S   "   �� 5T   &   " ��6,   # [ 6N   .�5U   *   -   5        5V   '   	 5X   "   �� 5Y   &   .R�5Z   *   �� $��Center=�5[   $   ��5Z   4   �� *Detail.Cena+ �� StringFormat$ {0:N2} руб.��5[   $   5\   '   5^      5_      5a      �� 5d      $��3��6    �� Orientation$ 
Horizontal=�6*   $��Right=�6C   $��20,20q�6_   $��Center=�6n   .+�5e      �� -   5        $��Отмена��5e      $	��120��6.   $��35��6:   $��0,0,10,0q�6F   $��2q�6X   ���	#FF3D4154���	#FF3D4154
�������$
��Bold"�5f   w    � Cursor$
 Hand��6�   $��15(�6�   �� 5g      -   5        $��Выбрать��5g      $	��120��6/   $��35��6;     LoginButtonStyle#��[ 5h      $��15(�6_   5i      5j      5k      5l      5m      