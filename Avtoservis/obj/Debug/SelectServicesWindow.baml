   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5   	   %    Avtoservis.SelectServicesWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:Avtoservis     5   	   -   5          � Title$  Выбор услуг��5   	   $	��640��6   $
��1050��6*   $	��640��67   $
��1050��6G    � WindowStartupLocation$ CenterScreen=�5	   	   $��Century Gothic)�5
   	   $��16(�6%   .��5      � ���	#FF282B38.+�5   
   �� 
������� �� CornerRadius$ 15��6$   $��20q�66   ._�5      �  � RowDefinitions 5      �� 5      $
��auto �6$   �� 5      $
��auto �6$   �� 5      $
��360* �6$   �� 5      $	��75* �6$   .+�5      �� $��Услуги��6   $��24(�6*   $
��Bold"�68   $��30,20,20,10q�5      $��0��61   ���	#FF567BFF6>   �� 5      -   5         ��Name$ SearchTextBox��5      $��1��61      SearchTextBoxStyle#��[  6>   $��Поиск...��5      $��20,10,20,10q�6*   $	��300��6?   $
��Left=�6K   $��Center=�6f   5   T   �� 5      -   5         ��Name$ ServicesList��5      $��2��61   $��
20,10,20,0q�6>     WorksListViewStyle#��[ 5       $��Stretch=�5!       �� SelectionMode$ Single=�5"      $��Disabled=�5#      $	��350��5$       �� View 5&      �� 5'      .P�5)      �� $��Выбрать��6-    �� Width$
 Auto��6>   	 �� CellTemplate	 5*   "   �� 5+   &   .R�5,   *   �� $��Center=�6T   $��Center=�5-   3   
 d� 	IsChecked
 5,   3   �� *
IsSelected+5-   3   5.   '   50      �� 53      $��Услуга��6-    �� HeaderContainerStyle 54   "   �� 55   &    �� 
TargetType" ��6,     SortableHeaderStyle �� BasedOn# [ 6N   .�56   *   -   5        57   '   	 59   "   �� 5:   &   .R�5;   *   �� $��Center=�5<   4   $��Center=�6O   $
��Wrap=�6l    �� TextAlignment$ Center=�6�   $
��10,0q�5=   4   ��5;   4   �� *Service.Naimenovanie+5=   4   5>   '   5@      �� 5C      $��Стоимость��6-    5D   "   �� 5E   &   " ��6,   # [ 6N   .�5F   *   -   5        5G   '   	 5I   "   �� 5J   &   .R�5K   *   �� $��Center=�5L   4   $��Center=�5M   4   $��0,0,10,0q�5N   4   ��5K   4   �� *Service.Stoimost+ �� StringFormat$ {0:N2} руб.��5N   4   5O   '   5Q      �� 5T      $'��!Длительность, мин.��6-    5U   "   �� 5V   &   " ��6,   # [ 6N   .�5W   *   -   5        5X   '   	 5Z   "   �� 5[   &   .R�5\   *   �� $��Center=�5]   4   $��Center=�5^   4   ��5\   4   �� *Service.Dlitelnost+5^   4   5_   '   5a      5b      5d      �� 5g      $��3��6    �� Orientation$ 
Horizontal=�6*   $��Right=�6C   $��20,20q�6_   $��Center=�6n   .+�5h      �� -   5        $��Отмена��5h      $	��120��6.   $��35��6:   $��0,0,10,0q�6F   $��2q�6X   ���	#FF3D4154���	#FF3D4154
�������$
��Bold"�5i   w    � Cursor$
 Hand��6�   $��15(�6�   �� 5j      -   5        $��Выбрать��5j      $	��120��6/   $��35��6;     LoginButtonStyle#��[ 5k      $��15(�6_   5l      5m      5n      5o      5p      