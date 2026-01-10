import React, { Component } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
import { icons, kinds } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';

class Diagnostics extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isLoading: true,
      isPushing: false,
      status: null,
      result: null,
      error: null
    };
  }

  componentDidMount() {
    this.fetchStatus();
  }

  //
  // Actions

  fetchStatus = () => {
    const { request } = createAjaxRequest({
      url: '/diagnostics/status',
      method: 'GET',
      dataType: 'json',
      skipDiagnostics: true
    });

    request.done((data) => {
      this.setState({
        status: data,
        isLoading: false
      });
    });

    request.fail(() => {
      this.setState({
        isLoading: false,
        error: translate('DiagnosticsStatusLoadFailed')
      });
    });
  };

  onPushPress = async () => {
    this.setState({ isPushing: true, error: null, result: null });

    if (window.ReadarrDiagnostics?.flush) {
      await window.ReadarrDiagnostics.flush();
    }

    const { request } = createAjaxRequest({
      url: '/diagnostics/push',
      method: 'POST',
      dataType: 'json',
      data: JSON.stringify({}),
      skipDiagnostics: true
    });

    request.done((data) => {
      this.setState({
        isPushing: false,
        result: data
      });
    });

    request.fail(() => {
      this.setState({
        isPushing: false,
        error: translate('DiagnosticsPushFailed')
      });
    });
  };

  //
  // Render

  render() {
    const {
      isLoading,
      isPushing,
      status,
      result,
      error
    } = this.state;

    const isDevelop = status?.isDevelop;
    const isConfigured = status?.hasToken && status?.repo;

    return (
      <PageContent title={translate('Diagnostics')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('PushDiagnostics')}
              iconName={icons.BUG}
              isSpinning={isPushing}
              isDisabled={!isDevelop || !isConfigured || isPushing}
              onPress={this.onPushPress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody>
          {
            isLoading &&
              <LoadingIndicator />
          }

          {
            !isLoading && error &&
              <Alert kind={kinds.DANGER}>
                {error}
              </Alert>
          }

          {
            !isLoading && !isDevelop &&
              <Alert kind={kinds.WARNING}>
                {translate('DiagnosticsDevelopOnly')}
              </Alert>
          }

          {
            !isLoading && isDevelop && !isConfigured &&
              <Alert kind={kinds.WARNING}>
                {translate('DiagnosticsNotConfigured')}
              </Alert>
          }

          {
            !isLoading && result?.message &&
              <Alert kind={result.success ? kinds.SUCCESS : kinds.DANGER}>
                {result.message}
              </Alert>
          }

          {
            !isLoading && status &&
              <FieldSet legend={translate('Diagnostics')}>
                <DescriptionList>
                  <DescriptionListItemTitle>{translate('Branch')}</DescriptionListItemTitle>
                  <DescriptionListItemDescription>
                    {window.Readarr.branch}
                  </DescriptionListItemDescription>

                  <DescriptionListItemTitle>{translate('DiagnosticsRepo')}</DescriptionListItemTitle>
                  <DescriptionListItemDescription>
                    {status.repo || translate('DiagnosticsRepoMissing')}
                  </DescriptionListItemDescription>

                  <DescriptionListItemTitle>{translate('DiagnosticsToken')}</DescriptionListItemTitle>
                  <DescriptionListItemDescription>
                    {status.hasToken ? translate('DiagnosticsTokenConfigured') : translate('DiagnosticsTokenMissing')}
                  </DescriptionListItemDescription>

                  <DescriptionListItemTitle>{translate('DiagnosticsLastFolder')}</DescriptionListItemTitle>
                  <DescriptionListItemDescription>
                    {result?.folder || translate('DiagnosticsNoPushYet')}
                  </DescriptionListItemDescription>
                </DescriptionList>
              </FieldSet>
          }
        </PageContentBody>
      </PageContent>
    );
  }
}

export default Diagnostics;
