import 'package:flutter_test/flutter_test.dart';
import 'package:kuran_app/main.dart';

void main() {
  testWidgets('App smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const KuranApp());
    expect(find.text('Kuran-ı Kerim'), findsOneWidget);
  });
}