   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5   	       Avtoservis.CaptchaWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:Avtoservis     5   	     � Title$/  )Проверка безопасности��5   	   $	��400��5	   	   $	��500��6    � WindowStartupLocation$ CenterOwner=�5
   	    � 
ResizeMode$ NoResize=�5   	    � Topmost .  � WindowStyle$
 None=�5   	   $��Century Gothic)�6   .��5      �� ���	#FF2D2D30 �� CornerRadius$ 10��6$   ���	#FF007ACC$��2q�6N   ._�5   
   � $��20q�6    � RowDefinitions 5      �� 5      $
��Auto �6    �� 5      $	��180 �6    �� 5      $
��Auto �6    �� 5      $
��Auto �6    �� 5      $��50 �6    .+�5      �� $9��3Подтвердите, что вы не робот��6   
�������$��18(�5   *   $��Center=�5      $��0��64   $
��Bold"�6A   �� 5      
������� �� Padding$ 10q�6(   $ 5��65   $��1��6F   $
��0,10q�6S   ._�5      � -   5         �Name$ CaptchaContainer��5      $	��400��61   $	��120��6=   .+�5      �� -   5        	 ��Name$	 CanvasNoise��5      
�������62   �� 5      -   5        
 ��Name$
 SPanelSymbols��5   !    �� Orientation$ 
Horizontal=�68   $��Center=�5       $��Center=�69   �� 5!      -   5        $	 CanvasStrikethrough��5!      5"      5#      �� 5%      $ 
Horizontal=�6   $��Center=�62   $��2��6O   $
��0,15q�6\   .+�5&      �� -   5         ��Name$ TextBoxCaptcha��5&      $	��250��62   $��35��6>   $��16(�6J   $��4,5,0,0q�6X   
����������	#FFABADB36}   �� 5'      -   5         ��Name$ BtnUpdateCaptcha��5'      $��35��63   $��35��6>   $��10,0,0,0q�6J    � Cursor$
 Hand��6\      RoundedButtonStyle#��[  5(   9   
����������	#FFABADB3$	��0.8q�6�   .��5)      �� $��Images/icon-reload.png��6   $��20��6<   $��20��6G   5*      5+      �� 5-      -   5         ��Name$ TextBlockTimer��5-      
���  ��$
��Bold"�6A   $��Center=�5.      $��3��65   $
��0,10q�6B   $y�	Collapsed=�6P   �� 50      -   5        $ BtnContinue��50      $��Продолжить��6*   $	��150��6?   $��35��6K   $��4��6W   $��0,15,0,0q�51   0   $
 Hand��6B     LoginButtonStyle#��[ 52      53      54      55      